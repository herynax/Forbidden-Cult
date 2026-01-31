using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
using Lean.Localization; // ДОБАВЛЕНО

public class MiniGameButton : MonoBehaviour
{
    public static List<MiniGameButton> allActiveButtons = new List<MiniGameButton>();

    public MiniGameSO gameData;

    [Header("UI References")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject readyVisual;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Localization Terms")]
    // Позволяет выбирать ключи прямо из выпадающего списка в инспекторе
    [LeanTranslationName][SerializeField] private string warningTerm;
    [LeanTranslationName][SerializeField] private string playButtonTerm = "UI_Play";

    [Header("Warning Text Settings")]
    [SerializeField] private TextMeshProUGUI warningTextUI;
    [SerializeField] private float warningDuration = 3f;

    [Header("Balance")]
    public float clickCooldownReduction = 1f;

    private Button mainButton;
    private SaveManager saveManager;
    private bool isReady;
    private bool hasRevealed;
    private Coroutine logicRoutine;

    private float currentRemainingCooldown;
    private bool warningShownInCurrentCycle = false;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        mainButton = GetComponent<Button>();
        if (warningTextUI != null) warningTextUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!allActiveButtons.Contains(this)) allActiveButtons.Add(this);
        SetupInitialState();
        if (logicRoutine != null) StopCoroutine(logicRoutine);
        logicRoutine = StartCoroutine(ButtonLogicRoutine());
    }

    private void OnDisable()
    {
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
            // Установка текста по ключу через код
            warningTextUI.text = LeanLocalization.GetTranslationText(warningTerm);
        }

        if (readyVisual != null)
        {
            readyVisual.transform.localScale = Vector3.zero;
            readyVisual.SetActive(false);
        }
    }

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
            if (timerText != null) timerText.transform.DOKill(true);
            if (timerText != null) timerText.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
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
        if (warningTextUI == null) yield break;

        warningTextUI.gameObject.SetActive(true);
        warningShownInCurrentCycle = true;

        // Берем свежий перевод в момент появления
        warningTextUI.text = LeanLocalization.GetTranslationText(warningTerm);

        warningTextUI.DOKill();
        warningTextUI.DOFade(1f, 0.5f);

        yield return new WaitForSeconds(warningDuration);

        warningTextUI.DOFade(0f, 3f)
            .SetLink(warningTextUI.gameObject)
            .OnComplete(() => {
                if (warningTextUI != null) warningTextUI.gameObject.SetActive(false);
            });
    }

    private void SetReady()
    {
        isReady = true;
        mainButton.interactable = true;

        if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);
        if (warningTextUI != null) warningTextUI.alpha = 0;

        transform.DOKill();
        if (timerText != null)
        {
            timerText.transform.DOKill();
            // Локализация кнопки "ИГРАТЬ!"
            timerText.text = LeanLocalization.GetTranslationText(playButtonTerm);
        }

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
            transform.DOKill();
            saveManager.Save();
            SceneLoader.Instance.LoadScene(gameData.SceneName);
        }
    }
}