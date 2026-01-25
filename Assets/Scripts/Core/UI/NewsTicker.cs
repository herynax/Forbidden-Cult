using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class NewsTicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI[] textElements; // Массив из 2-х текстов
    [SerializeField] private CanvasGroup[] textGroups;      // Массив из 2-х CanvasGroup
    [SerializeField] private NewsDataSO newsData;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f; // Скорость поворота колеса
    [SerializeField] private float displayDuration = 10f;      // Сколько висит новость
    [SerializeField] private float slideOffset = 50f;        // На сколько пикселей текст улетает вверх/вниз

    private int activeIndex = 0;
    private SaveManager saveManager;
    private bool isFirstStart = true;

    private void Awake()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();

        // Скрываем второй текст сразу
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

        // 1. Выбираем новую новость для следующего текста
        textElements[nextIndex].text = GetRandomValidNews();

        // 2. Устанавливаем начальную позицию для выезжающего текста (снизу)
        RectTransform nextRect = textElements[nextIndex].rectTransform;
        nextRect.anchoredPosition = new Vector2(0, -slideOffset);
        textGroups[nextIndex].alpha = 0;

        // 3. Анимация уезда ТЕКУЩЕГО текста (вверх и исчезновение)
        RectTransform activeRect = textElements[activeIndex].rectTransform;
        activeRect.DOAnchorPos(new Vector2(0, slideOffset), transitionDuration).SetEase(Ease.InQuart);
        textGroups[activeIndex].DOFade(0, transitionDuration);

        // 4. Анимация заезда НОВОГО текста (в центр и появление)
        nextRect.DOAnchorPos(Vector2.zero, transitionDuration).SetEase(Ease.OutQuart);
        textGroups[nextIndex].DOFade(1, transitionDuration);

        // 5. Меняем индекс
        activeIndex = nextIndex;
    }

    private string GetRandomValidNews()
    {
        if (newsData == null) return "Новостей пока нет...";

        var validNews = newsData.allNews
            .Where(n => IsNewsValid(n))
            .ToList();

        if (validNews.Count == 0) return "В мире всё спокойно...";

        return validNews[Random.Range(0, validNews.Count)].message;
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