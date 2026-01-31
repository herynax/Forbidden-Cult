using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Lean.Localization;

public class NewsTicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI[] textElements;
    [SerializeField] private CanvasGroup[] textGroups;
    [SerializeField] private NewsDataSO newsData;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private float displayDuration = 10f;
    [SerializeField] private float slideOffset = 50f;

    [Header("Default Terms")]
    [LeanTranslationName][SerializeField] private string noNewsTerm = "News_Empty";
    [LeanTranslationName][SerializeField] private string defaultNewsTerm = "News_Default";

    private int activeIndex = 0;
    private SaveManager saveManager;

    // Переменная для хранения ключа текущей новости
    private string currentActiveTerm;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();

        textGroups[0].alpha = 1;
        textGroups[1].alpha = 0;
    }

    private void OnEnable()
    {
        // Подписываемся на событие смены языка
        LeanLocalization.OnLocalizationChanged += UpdateCurrentTranslation;
    }

    private void OnDisable()
    {
        // Отписываемся при выключении
        LeanLocalization.OnLocalizationChanged -= UpdateCurrentTranslation;
    }

    private void Start()
    {
        ShowInitialNews();
        InvokeRepeating(nameof(RotateWheel), displayDuration, displayDuration);
    }

    // Метод, который вызывается автоматически при смене языка
    private void UpdateCurrentTranslation()
    {
        if (string.IsNullOrEmpty(currentActiveTerm)) return;

        // Обновляем текст в активном элементе по запомненному ключу
        textElements[activeIndex].text = LeanLocalization.GetTranslationText(currentActiveTerm);
    }

    private void ShowInitialNews()
    {
        currentActiveTerm = GetRandomValidTerm();
        textElements[activeIndex].text = LeanLocalization.GetTranslationText(currentActiveTerm);
    }

    private void RotateWheel()
    {
        int nextIndex = (activeIndex == 0) ? 1 : 0;

        // Получаем НОВЫЙ ключ
        currentActiveTerm = GetRandomValidTerm();
        // Устанавливаем текст для выезжающего элемента
        textElements[nextIndex].text = LeanLocalization.GetTranslationText(currentActiveTerm);

        // Анимации
        RectTransform nextRect = textElements[nextIndex].rectTransform;
        nextRect.anchoredPosition = new Vector2(0, -slideOffset);
        textGroups[nextIndex].alpha = 0;

        RectTransform activeRect = textElements[activeIndex].rectTransform;
        activeRect.DOAnchorPos(new Vector2(0, slideOffset), transitionDuration).SetEase(Ease.InQuart);
        textGroups[activeIndex].DOFade(0, transitionDuration);

        nextRect.DOAnchorPos(Vector2.zero, transitionDuration).SetEase(Ease.OutQuart);
        textGroups[nextIndex].DOFade(1, transitionDuration);

        activeIndex = nextIndex;
    }

    // Теперь метод возвращает КЛЮЧ (string), а не переведенный текст
    private string GetRandomValidTerm()
    {
        if (newsData == null || newsData.allNews.Count == 0)
            return noNewsTerm;

        var validEntries = newsData.allNews
            .Where(n => IsNewsValid(n))
            .ToList();

        if (validEntries.Count == 0)
            return defaultNewsTerm;

        NewsEntry selectedEntry = validEntries[Random.Range(0, validEntries.Count)];
        return selectedEntry.messageTerm;
    }

    private bool IsNewsValid(NewsEntry entry)
    {
        if (saveManager == null || saveManager.data == null) return true;

        bool isUnlocked = string.IsNullOrEmpty(entry.unlockID) ||
                          saveManager.data.GetUpgradeCount(entry.unlockID) > 0;

        bool isNotHidden = string.IsNullOrEmpty(entry.hideID) ||
                           saveManager.data.GetUpgradeCount(entry.hideID) == 0;

        return isUnlocked && isNotHidden;
    }
}