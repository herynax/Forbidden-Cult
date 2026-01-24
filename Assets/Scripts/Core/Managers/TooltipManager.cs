using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

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

    private SaveManager saveManager; // ДОБАВЛЕНО: Ссылка на менеджер сохранений
    private UpgradeSO currentShownUpgrade;
    private bool isActive = false;
    private RectTransform canvasRect;
    private Canvas parentCanvas;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0;

        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas.GetComponent<RectTransform>();

        // ДОБАВЛЕНО: Инициализация ссылки
        saveManager = Object.FindFirstObjectByType<SaveManager>();
    }

    public void ShowTooltip(UpgradeSO data, int currentCount)
    {
        isActive = true;
        currentShownUpgrade = data;

        nameText.text = data.Name;
        descriptionText.text = data.Description;
        loreText.text = "<i>\"" + data.LoreText + "\"</i>";

        RefreshDynamicData();

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.15f).SetUpdate(true);
    }

    private void RefreshDynamicData()
    {
        // Проверка безопасности: если менеджер не найден или данные не прогрузились
        if (currentShownUpgrade == null || saveManager == null || saveManager.data == null) return;

        var state = saveManager.data.Upgrades.Find(u => u.ID == currentShownUpgrade.ID);
        int count = state != null ? state.Amount : 0;

        if (count > 0)
        {
            incomeText.gameObject.SetActive(true);

            double eachProvides = currentShownUpgrade.BasePassiveIncome;
            double totalProvides = eachProvides * count;

            // Форматируем текст дохода
            incomeText.text = $"Собрано скверны: <color=#B000FF>{BigNumberFormatter.Format(state.TotalEarned)}</color>\n" +
                              $"Всего приносит: <color=green>{BigNumberFormatter.Format(totalProvides)}</color>\n" +
                              $"Каждый дает: <color=green>{BigNumberFormatter.Format(eachProvides)}</color>";
;
        }
        else
        {
            incomeText.gameObject.SetActive(false);
        }
    }

    public void HideTooltip()
    {
        isActive = false;
        currentShownUpgrade = null; // Сбрасываем объект
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.15f).SetUpdate(true);
    }

    private void Update()
    {
        if (!isActive) return;

        // ВЫЗЫВАЕМ ОБНОВЛЕНИЕ ЦИФР каждый кадр, чтобы они "тикали" пока мы смотрим
        RefreshDynamicData();

        // Логика движения
        Vector2 localPoint;
        Camera uiCamera = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, uiCamera, out localPoint);

        float targetY = localPoint.y;
        float pivotOffset = tooltipRect.rect.height * tooltipRect.pivot.y;
        float canvasHalfHeight = canvasRect.rect.height / 2f;

        float minY = -canvasHalfHeight + pivotOffset;
        float maxY = canvasHalfHeight - (tooltipRect.rect.height - pivotOffset);
        targetY = Mathf.Clamp(targetY, minY, maxY);

        Vector2 targetPos = new Vector2(tooltipRect.anchoredPosition.x, targetY);
        tooltipRect.anchoredPosition = Vector2.Lerp(tooltipRect.anchoredPosition, targetPos, Time.deltaTime * followSpeed);
    }
}