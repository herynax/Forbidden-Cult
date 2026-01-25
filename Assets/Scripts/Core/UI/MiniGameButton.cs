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
    [SerializeField] private GameObject readyVisual;
    [SerializeField] private GameObject warningVisual;
    [SerializeField] private CanvasGroup canvasGroup;

    private SaveManager saveManager;
    private bool isReady;
    private bool hasRevealed;
    private bool isWarningStarted;
    private Coroutine logicRoutine;

    private void Awake()
    {
        // Лучше искать менеджер в Awake
        saveManager = Object.FindFirstObjectByType<SaveManager>();
    }

    private void OnEnable()
    {
        // Сбрасываем визуал при включении (если еще не раскрыта)
        if (!hasRevealed) transform.localScale = Vector3.zero;

        if (logicRoutine != null) StopCoroutine(logicRoutine);
        logicRoutine = StartCoroutine(ButtonLogicRoutine());
    }

    private void OnDisable()
    {
        if (logicRoutine != null) StopCoroutine(logicRoutine);
        // Убиваем твины, чтобы они не выдавали ошибки при выключении объекта
        transform.DOKill();
        timerText.DOKill();
    }

    private void Start()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();

        // Начальное состояние кнопки
        transform.localScale = Vector3.zero;

        // Настройка FX (Ready Visual)
        if (readyVisual != null)
        {
            readyVisual.transform.localScale = Vector3.zero; // Скейл в 0
            readyVisual.SetActive(false);
        }

        cooldownFill.fillAmount = 0;
        timerText.text = "";
        warningVisual.SetActive(false);

        // Запуск логики через OnEnable (чтобы не было ошибок неактивного объекта)
    }

    private IEnumerator ButtonLogicRoutine()
    {
        // Ждем пока данные загрузятся
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
                if (!hasRevealed)
                {
                    yield return RevealButton();
                }

                if (!isReady)
                {
                    yield return StartCooldown();
                }
            }
            else
            {
                // Если условия перестали соблюдаться (например, сброс сейва)
                transform.localScale = Vector3.zero;
                hasRevealed = false;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator RevealButton()
    {
        hasRevealed = true;
        transform.DOKill();
        yield return transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    private IEnumerator StartCooldown()
    {
        float duration = Random.Range(gameData.minCooldown, gameData.maxCooldown);
        float elapsed = 0;
        isWarningStarted = false;

        if (canvasGroup != null) canvasGroup.alpha = 0.6f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;

            cooldownFill.fillAmount = elapsed / duration;

            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

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
        isWarningStarted = true;
        warningVisual.SetActive(true);
        timerText.DOColor(Color.red, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        transform.DOShakeRotation(10f, 2f, 10, 90).SetLoops(-1).SetLink(gameObject);
    }

    private void SetReady()
    {
        isReady = true;
        isWarningStarted = false;

        // Останавливаем тряску и красные тексты
        transform.DOKill();
        timerText.DOKill();
        warningVisual.SetActive(false);

        timerText.text = "ИГРАТЬ!";
        timerText.color = Color.white;

        if (canvasGroup != null) canvasGroup.DOFade(1f, 0.3f);

        // --- ЛОГИКА FX (Ready Visual) ---
        if (readyVisual != null)
        {
            readyVisual.SetActive(true);
            readyVisual.transform.DOKill(); // Убиваем старые твины на FX
            readyVisual.transform.localScale = Vector3.zero; // Гарантируем 0 перед стартом

            // Плавное появление до 0.3 через OutCubic
            readyVisual.transform.DOScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true); // Чтобы работало даже при паузе
        }

        // Саму кнопку можно тоже слегка увеличить для акцента
        transform.DOScale(1.05f, 0.3f).SetEase(Ease.OutBack);
    }

    public void OnClick()
    {
        if (isReady)
        {
            isReady = false; // Чтобы нельзя было нажать дважды
            transform.DOKill();
            saveManager.Save();
            SceneLoader.Instance.LoadScene(gameData.SceneName);
        }
    }
}