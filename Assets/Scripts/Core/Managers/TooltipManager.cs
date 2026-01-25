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

    private SaveManager saveManager;
    private UpgradeSO currentShownUpgrade;
    private bool isActive = false;
    private RectTransform canvasRect;
    private Canvas parentCanvas;

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
        // 1. Проверяем, открыто ли улучшение (есть ли оно в списке RevealedUpgrades)
        bool isRevealed = saveManager.data.RevealedUpgrades.Contains(data.ID);

        canvasGroup.gameObject.SetActive(true);
        isActive = true;
        currentShownUpgrade = data;

        if (!isRevealed)
        {
            // СОСТОЯНИЕ: Скрыто (???)
            nameText.text = "???";
            descriptionText.text = "Соберите больше скверны, чтобы узреть это...";
            loreText.text = "";
            incomeText.gameObject.SetActive(false);
        }
        else
        {
            // СОСТОЯНИЕ: Раскрыто
            nameText.text = data.Name;
            descriptionText.text = data.Description;
            loreText.text = "<i>\"" + data.LoreText + "\"</i>";

            // Метод сам решит, показывать ли доход (если куплено > 0)
            RefreshDynamicData();
        }

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.15f).SetUpdate(true);
    }

    private void RefreshDynamicData()
    {
        if (currentShownUpgrade == null || saveManager == null || saveManager.data == null) return;

        // Если улучшение еще не раскрыто, не обновляем цифры дохода
        if (!saveManager.data.RevealedUpgrades.Contains(currentShownUpgrade.ID)) return;

        var state = saveManager.data.Upgrades.Find(u => u.ID == currentShownUpgrade.ID);
        int count = state != null ? state.Amount : 0;

        if (count > 0)
        {
            incomeText.gameObject.SetActive(true);

            double eachProvides = currentShownUpgrade.BasePassiveIncome;
            double totalProvides = eachProvides * count;

            incomeText.text = $"Собрано скверны: <color=#B000FF>{BigNumberFormatter.Format(state.TotalEarned)}</color>\n" +
                              $"Всего приносит: <color=green>{BigNumberFormatter.Format(totalProvides)}</color>\n" +
                              $"Каждый дает: <color=green>{BigNumberFormatter.Format(eachProvides)}</color>";
        }
        else
        {
            incomeText.gameObject.SetActive(false);
        }
    }

    public void HideTooltip()
    {
        isActive = false;
        currentShownUpgrade = null;

        canvasGroup.DOKill();
        // Плавно исчезаем и ТОЛЬКО ПОТОМ выключаем объект через OnComplete
        canvasGroup.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() => {
            if (!isActive) canvasGroup.gameObject.SetActive(false);
        });
    }

    private void Update()
    {
        if (!isActive) return;

        RefreshDynamicData();

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