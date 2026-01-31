using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Lean.Localization;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("References")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private TextMeshProUGUI incomeText;

    [Header("Settings")]
    [SerializeField] private float followSpeed = 25f;

    private SaveManager saveManager;
    private UpgradeSO currentShownUpgrade;
    private UpgradeState currentUpgradeState; // КЭШИРУЕМ СОСТОЯНИЕ
    private bool isActive = false;
    private RectTransform canvasRect;
    private Canvas parentCanvas;

    // Таймер для ограничения частоты обновления текста
    private float textUpdateTimer = 0f;
    private const float textUpdateRate = 0.1f; // Обновлять 10 раз в секунду

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0;
        canvasGroup.gameObject.SetActive(false);

        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas.GetComponent<RectTransform>();
        saveManager = Object.FindFirstObjectByType<SaveManager>();
    }

    public void ShowTooltip(UpgradeSO data, int currentCount)
    {
        // 1. ВКЛЮЧАЕМ ОБЪЕКТ (это то, чего не хватало)
        canvasGroup.gameObject.SetActive(true);

        isActive = true;
        currentShownUpgrade = data;

        // Кэшируем состояние сразу, чтобы не искать в Update
        currentUpgradeState = saveManager.data.Upgrades.Find(u => u.ID == data.ID);

        bool isRevealed = saveManager.data.RevealedUpgrades.Contains(data.ID);

        if (!isRevealed)
        {
            nameText.text = LeanLocalization.GetTranslationText("UI_Unknown");
            descriptionText.text = LeanLocalization.GetTranslationText("UI_NeedMoreCorruption");
            loreText.text = "";
            incomeText.gameObject.SetActive(false);
        }
        else
        {
            nameText.text = LeanLocalization.GetTranslationText(data.NameTerm);
            descriptionText.text = LeanLocalization.GetTranslationText(data.DescriptionTerm);
            loreText.text = "<i>\"" + LeanLocalization.GetTranslationText(data.LoreTerm) + "\"</i>";

            RefreshDynamicData();
        }

        // Анимация появления
        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.15f).SetUpdate(true);
    }


    private void RefreshDynamicData()
    {
        if (currentShownUpgrade == null || saveManager == null) return;

        var state = saveManager.data.Upgrades.Find(u => u.ID == currentShownUpgrade.ID);
        int count = state != null ? state.Amount : 0;

        if (count > 0)
        {
            incomeText.gameObject.SetActive(true);

            // Вытягиваем только названия действий/меток
            string txtEarned = LeanLocalization.GetTranslationText("Stat_TotalEarned"); // "Собрано скверны"
            string txtProvides = LeanLocalization.GetTranslationText("Stat_TotalProvides"); // "Всего приносит"
            string txtEach = LeanLocalization.GetTranslationText("Stat_EachProvides"); // "Каждый дает"

            // Собираем строку через F (интерполяцию), полностью контролируя верстку
            incomeText.text = $"{txtEarned}: <color=#B000FF>{BigNumberFormatter.Format(state.TotalEarned)}</color>\n" +
                              $"{txtProvides}: <color=green>{BigNumberFormatter.Format(currentShownUpgrade.BasePassiveIncome * count)}</color>\n" +
                              $"{txtEach}: <color=green>{BigNumberFormatter.Format(currentShownUpgrade.BasePassiveIncome)}</color>";
        }
        else
        {
            incomeText.gameObject.SetActive(false);
        }
    }

    public void HideTooltip()
    {
        // Сразу ставим флаг в false, чтобы Update перестал дергать расчеты и движение
        isActive = false;
        currentShownUpgrade = null;
        currentUpgradeState = null;

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() => {
            // Выключаем объект только если за время анимации мы не навели на новую кнопку
            if (!isActive)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        });
    }

    private void Update()
    {
        if (!isActive) return;

        // ОБНОВЛЕНИЕ ТЕКСТА ПО ТАЙМЕРУ (оптимизация UI)
        textUpdateTimer += Time.deltaTime;
        if (textUpdateTimer >= textUpdateRate)
        {
            textUpdateTimer = 0f;
            RefreshDynamicData();
        }

        // Логика движения (без изменений)
        Vector2 localPoint;
        Camera uiCamera = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, uiCamera, out localPoint);

        float targetY = localPoint.y;
        float pivotOffset = tooltipRect.rect.height * tooltipRect.pivot.y;
        float canvasHalfHeight = canvasRect.rect.height / 2f;

        targetY = Mathf.Clamp(targetY, -canvasHalfHeight + pivotOffset, canvasHalfHeight - (tooltipRect.rect.height - pivotOffset));

        Vector2 targetPos = new Vector2(tooltipRect.anchoredPosition.x, targetY);
        tooltipRect.anchoredPosition = Vector2.Lerp(tooltipRect.anchoredPosition, targetPos, Time.deltaTime * followSpeed);
    }
}