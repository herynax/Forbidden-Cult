using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("References")]
    [SerializeField] private RectTransform tooltipRect; // Сама плашка тултипа
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private TextMeshProUGUI incomeText;

    [Header("Settings")]
    [SerializeField] private float followSpeed = 25f;

    private bool isActive = false;
    private RectTransform canvasRect;
    private Canvas parentCanvas;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0;

        // Находим канвас и его RectTransform
        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas.GetComponent<RectTransform>();
    }

    public void ShowTooltip(UpgradeSO data, int currentCount)
    {
        isActive = true;

        nameText.text = data.Name;
        descriptionText.text = data.Description;
        loreText.text = "<i>\"" + data.LoreText + "\"</i>";

        // Показываем доход только если куплено > 0
        if (currentCount > 0)
        {
            incomeText.gameObject.SetActive(true);
            double totalIncome = data.BasePassiveIncome * currentCount;
            incomeText.text = $"Всего приносит: <color=green>{BigNumberFormatter.Format(totalIncome)}</color>";
        }
        else
        {
            incomeText.gameObject.SetActive(false);
        }

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.15f).SetUpdate(true);
    }

    public void HideTooltip()
    {
        isActive = false;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.15f).SetUpdate(true);
    }

    private void Update()
    {
        if (!isActive) return;

        Vector2 localPoint;

        // ВАЖНО: Если Canvas в режиме Camera, нужно передавать worldCamera.
        // Если в Overlay — передавать null.
        Camera uiCamera = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            uiCamera,
            out localPoint
        );

        // Фиксируем X (берем текущий из инспектора), меняем только Y
        float targetY = localPoint.y;

        // Ограничиваем движение по Y, чтобы тултип не уходил за край экрана
        float pivotOffset = tooltipRect.rect.height * tooltipRect.pivot.y;
        float canvasHalfHeight = canvasRect.rect.height / 2f;

        // Зажимаем позицию между нижним и верхним краем канваса
        float minY = -canvasHalfHeight + pivotOffset;
        float maxY = canvasHalfHeight - (tooltipRect.rect.height - pivotOffset);
        targetY = Mathf.Clamp(targetY, minY, maxY);

        // Плавное перемещение
        Vector2 targetPos = new Vector2(tooltipRect.anchoredPosition.x, targetY);
        tooltipRect.anchoredPosition = Vector2.Lerp(tooltipRect.anchoredPosition, targetPos, Time.deltaTime * followSpeed);
    }
}