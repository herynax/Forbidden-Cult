using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro;
using DG.Tweening;

public class MiniGameButton : MonoBehaviour
{
    public MiniGameSO gameData;

    [Header("UI References")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject readyVisual;  // Твой FX
    [SerializeField] private CanvasGroup canvasGroup;

    private Button mainButton;
    private SaveManager saveManager;
    private bool isReady;
    private bool hasRevealed;
    private bool isWarningStarted;
    private Coroutine logicRoutine;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        mainButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Скрываем всё ПЕРЕД началом логики
        SetupInitialState();

        if (logicRoutine != null) StopCoroutine(logicRoutine);
        logicRoutine = StartCoroutine(ButtonLogicRoutine());
    }

    private void SetupInitialState()
    {
        // Сама кнопка
        transform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        if (mainButton != null) mainButton.interactable = false;

        // Дочерние элементы (Таймер и Заливка) по умолчанию выключены
        if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        // FX эффекты
        if (readyVisual != null)
        {
            readyVisual.transform.localScale = Vector3.zero;
            readyVisual.SetActive(false);
        }
    }

    private IEnumerator ButtonLogicRoutine()
    {
        // Ждем загрузки данных
        while (saveManager == null || saveManager.data == null)
        {
            saveManager = Object.FindFirstObjectByType<SaveManager>();
            yield return new WaitForSeconds(0.2f);
        }

        while (true)
        {
            // Проверка условий открытия
            bool conditionsMet = gameData.RequiredUpgradeIDs.All(id =>
                saveManager.data.Upgrades.Any(u => u.ID == id && u.Amount > 0));

            if (conditionsMet)
            {
                // ЭТАП 1: Появление кнопки
                if (!hasRevealed)
                {
                    yield return RevealButton();
                }

                // ЭТАП 2: Кулдаун
                if (!isReady)
                {
                    yield return StartCooldown();
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator RevealButton()
    {
        hasRevealed = true;

        // Включаем видимость через CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, 0.5f);
            canvasGroup.blocksRaycasts = true;
        }

        transform.DOKill();
        // Анимация масштаба всей кнопки 0 -> 1
        yield return transform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    private IEnumerator StartCooldown()
    {
        float duration = Random.Range(gameData.minCooldown, gameData.maxCooldown);
        float elapsed = 0;
        isWarningStarted = false;

        // ВКЛЮЧАЕМ Таймер и Заливку в момент начала КД
        if (cooldownFill != null) cooldownFill.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);

        mainButton.interactable = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;

            if (cooldownFill != null) cooldownFill.fillAmount = elapsed / duration;

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60);
                int seconds = Mathf.FloorToInt(remaining % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // За 10 секунд до конца - тряска
            if (remaining <= 10f && !isWarningStarted)
            {
                TriggerWarning();
            }
            yield return null;
        }

        SetReady();
    }

    private void TriggerWarning()
    {

        // Тряска и пульсация таймера
        timerText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f).SetLoops(-1);
        transform.DOShakeRotation(10f, 2f, 10).SetLoops(-1).SetLink(gameObject);
    }

    private void SetReady()
    {
        isReady = true;
        isWarningStarted = false;
        mainButton.interactable = true;

        // Выключаем элементы кулдауна
        if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);

        transform.DOKill();
        timerText.transform.DOKill();

        timerText.text = "ИГРАТЬ!";
        timerText.color = Color.white;

        // ЭТАП 3: FX (Ready Visual) выплывает из 0 до 0.3
        if (readyVisual != null)
        {
            readyVisual.SetActive(true);
            readyVisual.transform.localScale = Vector3.zero;
            readyVisual.transform.DOScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f)
                .SetEase(Ease.OutCubic);
        }

        // Анимация "Готовности" самой кнопки (подпрыгивание)
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f, 5)
            .SetLoops(-1, LoopType.Restart)
            .SetDelay(1f);
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

    private void OnDisable()
    {
        if (logicRoutine != null) StopCoroutine(logicRoutine);
        transform.DOKill();
        if (timerText != null) timerText.transform.DOKill();
        if (readyVisual != null) readyVisual.transform.DOKill();
    }
}