using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Linq;
using Lean.Localization; // Добавляем пространство имен локализации

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
    [LeanTranslationName][SerializeField] private string noNewsTerm = "News_Empty";    // Ключ "Новостей нет"
    [LeanTranslationName][SerializeField] private string defaultNewsTerm = "News_Default"; // Ключ "Все спокойно"

    private int activeIndex = 0;
    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();

        textGroups[0].alpha = 1;
        textGroups[1].alpha = 0;
    }

    private void Start()
    {
        ShowInitialNews();
        InvokeRepeating(nameof(RotateWheel), displayDuration, displayDuration);
    }

    private void ShowInitialNews()
    {
        textElements[activeIndex].text = GetRandomValidNews();
    }

    private void RotateWheel()
    {
        int nextIndex = (activeIndex == 0) ? 1 : 0;

        // Берем локализованный текст
        textElements[nextIndex].text = GetRandomValidNews();

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

    private string GetRandomValidNews()
    {
        if (newsData == null || newsData.allNews.Count == 0)
            return LeanLocalization.GetTranslationText(noNewsTerm);

        var validEntries = newsData.allNews
            .Where(n => IsNewsValid(n))
            .ToList();

        if (validEntries.Count == 0)
            return LeanLocalization.GetTranslationText(defaultNewsTerm);

        // Выбираем случайную запись
        NewsEntry selectedEntry = validEntries[Random.Range(0, validEntries.Count)];

        // Возвращаем перевод по ключу
        return LeanLocalization.GetTranslationText(selectedEntry.messageTerm);
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