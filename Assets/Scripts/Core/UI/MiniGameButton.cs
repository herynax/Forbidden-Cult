using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Для List
using System.Linq;
using TMPro;
using DG.Tweening;

public class MiniGameButton : MonoBehaviour
{
    // СТАТИЧЕСКИЙ СПИСОК для доступа ко всем кнопкам извне
    public static List<MiniGameButton> allActiveButtons = new List<MiniGameButton>();

    public MiniGameSO gameData;

    [Header("UI References")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject readyVisual;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Warning Text Settings")]
    [SerializeField] private TextMeshProUGUI warningTextUI; // Текст, который вводит человек
    [SerializeField] private string customWarningMessage = "Почти готово...";
    [SerializeField] private float warningDuration = 3f;

    [Header("Balance")]
    public float clickCooldownReduction = 1f; // Сколько секунд срезает один клик

    private Button mainButton;
    private SaveManager saveManager;
    private bool isReady;
    private bool hasRevealed;
    private Coroutine logicRoutine;

    // Переменная для управления временем кулдауна в реальном времени
    private float currentRemainingCooldown;
    private bool warningShownInCurrentCycle = false;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        mainButton = GetComponent<Button>();
        warningTextUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Регистрируем кнопку в глобальном списке
        if (!allActiveButtons.Contains(this)) allActiveButtons.Add(this);

        SetupInitialState();

        if (logicRoutine != null) StopCoroutine(logicRoutine);
        logicRoutine = StartCoroutine(ButtonLogicRoutine());
    }

    private void OnDisable()
    {
        // Удаляем кнопку из списка
        if (allActiveButtons.Contains(this)) allActiveButtons.Remove(this);

        if (logicRoutine != null) StopCoroutine(logicRoutine);
        transform.DOKill();
        if (timerText != null) timerText.transform.DOKill();
        if (readyVisual != null) readyVisual.transform.DOKill();
        if (warningTextUI != null) warningTextUI.transform.DOKill();
    }

    private void SetupInitialState()
    {
        transform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        if (mainButton != null) mainButton.interactable = false;

        if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (warningTextUI != null)
        {
            warningTextUI.alpha = 0;
            warningTextUI.text = customWarningMessage;
        }

        if (readyVisual != null)
        {
            readyVisual.transform.localScale = Vector3.zero;
            readyVisual.SetActive(false);
        }
    }

    // МЕТОД ДЛЯ ГЛАВНОГО КЛИКЕРА
    public static void ReduceAllCooldowns(float seconds)
    {
        foreach (var btn in allActiveButtons)
        {
            btn.ReduceCooldown(seconds);
        }
    }

    public void ReduceCooldown(float seconds)
    {
        if (hasRevealed && !isReady)
        {
            currentRemainingCooldown -= seconds;
            // Визуальный фидбек среза времени (легкая тряска таймера)
            timerText.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
        }
    }

    private IEnumerator ButtonLogicRoutine()
    {
        while (saveManager == null || saveManager.data == null)
        {
            saveManager = Object.FindFirstObjectByType<SaveManager>();
            yield return new WaitForSeconds(0.2f);
        }

        while (true)
        {
            bool conditionsMet = gameData.RequiredUpgradeIDs.All(id =>
                saveManager.data.Upgrades.Any(u => u.ID == id && u.Amount > 0));

            if (conditionsMet)
            {
                if (!hasRevealed) yield return RevealButton();
                if (!isReady) yield return StartCooldown();
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator RevealButton()
    {
        hasRevealed = true;
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, 0.5f);
            canvasGroup.blocksRaycasts = true;
        }
        transform.DOKill();
        yield return transform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    private IEnumerator StartCooldown()
    {
        float totalDuration = Random.Range(gameData.minCooldown, gameData.maxCooldown);
        currentRemainingCooldown = totalDuration;
        warningShownInCurrentCycle = false;

        if (cooldownFill != null) cooldownFill.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);

        mainButton.interactable = false;

        while (currentRemainingCooldown > 0)
        {
            currentRemainingCooldown -= Time.deltaTime;

            float fillValue = 1f - (currentRemainingCooldown / totalDuration);
            if (cooldownFill != null) cooldownFill.fillAmount = fillValue;

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(currentRemainingCooldown / 60);
                int seconds = Mathf.FloorToInt(currentRemainingCooldown % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // ЛОГИКА ПРЕДУПРЕЖДЕНИЯ (за 10 секунд)
            if (currentRemainingCooldown <= 10f && !warningShownInCurrentCycle)
            {
                StartCoroutine(ShowWarningText());
            }

            yield return null;
        }

        SetReady();
    }

    private IEnumerator ShowWarningText()
    {
        warningTextUI.gameObject.SetActive(true);

        warningShownInCurrentCycle = true;
        if (warningTextUI == null) yield break;

        warningTextUI.DOKill();
        // Появление
        warningTextUI.DOFade(1f, 0.5f);

        yield return new WaitForSeconds(warningDuration);

        // Исчезновение за 3 секунды, как ты просил
        warningTextUI.DOFade(0f, 3f)
            .SetLink(warningTextUI.gameObject)
            .OnComplete(() => warningTextUI.gameObject.SetActive(false));
    }

    private void SetReady()
    {
        isReady = true;
        mainButton.interactable = true;

        if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);
        if (warningTextUI != null) warningTextUI.alpha = 0;

        transform.DOKill();
        timerText.transform.DOKill();
        timerText.text = "ИГРАТЬ!";

        if (readyVisual != null)
        {
            readyVisual.SetActive(true);
            readyVisual.transform.localScale = Vector3.zero;
            readyVisual.transform.DOScale(new Vector3(0.2f, 0.2f, 0.2f), 0.5f).SetEase(Ease.OutCubic);
        }

        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f, 5).SetLoops(-1, LoopType.Restart).SetDelay(1f);
    }

    public void OnClick()
    {
        if (isReady)
        {
            isReady = false;
            hasRevealed = false; // Сброс для следующего круга после возврата
            transform.DOKill();
            saveManager.Save();
            SceneLoader.Instance.LoadScene(gameData.SceneName);
        }
    }
}