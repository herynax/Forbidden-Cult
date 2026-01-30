using UnityEngine;
using DG.Tweening;
using Lean.Localization; // Не забудь
using UnityEngine.UI;

public class LanguagePanelAnimator : MonoBehaviour
{
    [SerializeField] private GameObject languagePanel;
    [SerializeField] private GameObject languageFadeObject;
    [SerializeField] private CanvasGroup languageButtonsGroup;
    [SerializeField] private CanvasGroup languageFadeGroup;
    [SerializeField] private Transform languageContainer;

    private SaveManager saveManager;

    private void Awake()
    {
        // Начальное состояние: черный фон виден, кнопки прозрачные
        languageFadeGroup.alpha = 1;
        languageButtonsGroup.alpha = 0;
        languageButtonsGroup.interactable = false;
        languageButtonsGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();

        // Ждем инициализации данных
        if (saveManager != null && saveManager.data != null)
        {
            if (saveManager.data.languageChosen == false)
            {
                // Если зашел первый раз — показываем выбор
                ShowLanguageButtons();
            }
            else
            {
                // Если уже выбирал — мгновенно выключаем всё
                languageFadeObject.SetActive(false);
                languagePanel.SetActive(false);
            }
        }
    }

    // Этот метод вешается на кнопки выбора языка (RU / EN)
    // В параметре передаем имя языка из Lean Localization (например "Russian" или "English")
    public void SelectLanguage(string langName)
    {
        // 1. Меняем язык в системе
        LeanLocalization.SetCurrentLanguageAll(langName);

        // 2. Сохраняем выбор
        if (saveManager != null)
        {
            saveManager.data.languageChosen = true;
            saveManager.Save();
        }

        // 3. Убираем панель
        HideLanguageButtons();
    }

    public void ShowLanguageButtons()
    {
        languagePanel.SetActive(true);
        languageFadeObject.SetActive(true);

        languageButtonsGroup.DOKill();
        languageButtonsGroup.interactable = true;
        languageButtonsGroup.blocksRaycasts = true;
        languageButtonsGroup.DOFade(1f, 0.4f).SetUpdate(true).SetLink(gameObject);

        languageContainer.DOKill();
        languageContainer.localScale = Vector3.zero;
        languageContainer.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
    }

    public void HideLanguageButtons()
    {
        languageButtonsGroup.DOKill();
        languageButtonsGroup.interactable = false;
        languageButtonsGroup.blocksRaycasts = false;
        languageButtonsGroup.DOFade(0f, 0.4f).SetUpdate(true).SetLink(gameObject);

        languageContainer.DOKill();
        languageContainer.DOScale(0f, 0.4f).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject);

        languageFadeGroup.DOKill();
        languageFadeGroup.DOFade(0f, 0.8f)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                languageFadeObject.SetActive(false);
                languagePanel.SetActive(false);
            });
    }

    private void OnDestroy()
    {
        // Безопасная очистка
        languageButtonsGroup.DOKill();
        languageContainer.DOKill();
        languageFadeGroup.DOKill();
    }
}