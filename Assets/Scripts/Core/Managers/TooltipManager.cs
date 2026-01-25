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
        bool isRevealed = saveManager.data.RevealedUpgrades.Contains(data.ID);

        canvasGroup.gameObject.SetActive(true);
        isActive = true;
        currentShownUpgrade = data;

        // НАХОДИМ И ЗАПОМИНАЕМ СОСТОЯНИЕ (чтобы не искать каждый кадр)
        currentUpgradeState = saveManager.data.Upgrades.Find(u => u.ID == data.ID);

        if (!isRevealed)
        {
            nameText.text = "???";
            descriptionText.text = "Соберите больше скверны, чтобы узреть это...";
            loreText.text = "";
            incomeText.gameObject.SetActive(false);
        }
        else
        {
            nameText.text = data.Name;
            descriptionText.text = data.Description;
            loreText.text = "<i>\"" + data.LoreText + "\"</i>";

            RefreshDynamicData();
        }

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.15f).SetUpdate(true);
    }

    private void RefreshDynamicData()
    {
        if (currentShownUpgrade == null || saveManager == null) return;
        if (!saveManager.data.RevealedUpgrades.Contains(currentShownUpgrade.ID)) return;

        // Используем закэшированное состояние. Если его нет (еще не купили), то count = 0
        int count = currentUpgradeState != null ? currentUpgradeState.Amount : 0;

        if (count > 0)
        {
            incomeText.gameObject.SetActive(true);

            double eachProvides = currentShownUpgrade.BasePassiveIncome;
            double totalProvides = eachProvides * count;

            // currentUpgradeState.TotalEarned обновляется в PassiveIncomeManager
            incomeText.text = $"Собрано скверны: <color=#B000FF>{BigNumberFormatter.Format(currentUpgradeState.TotalEarned)}</color>\n" +
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
        currentUpgradeState = null; // Сбрасываем кэш

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() => {
            if (!isActive) canvasGroup.gameObject.SetActive(false);
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