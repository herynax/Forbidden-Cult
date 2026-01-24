using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UpgradeSO upgradeSO;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    private SaveManager saveManager;
    private double currentPrice;
    private bool isRevealed;

    public void Init(SaveManager sm)
    {
        saveManager = sm;
        isRevealed = saveManager.data.IsRevealed(upgradeSO.ID);
        RefreshUI();
    }

    private void Update()
    {
        // Считаем цену
        int count = saveManager.data.GetUpgradeCount(upgradeSO.ID);
        currentPrice = upgradeSO.BasePrice * Mathf.Pow(1.15f, count); //x = 15 * 1.15^y

        // Логика раскрытия (70% от цены)
        if (!isRevealed && saveManager.data.Money >= currentPrice)
        {
            Reveal();
        }

        priceText.text = BigNumberFormatter.StoreFormat(currentPrice);
        priceText.color = saveManager.data.Money >= currentPrice ? Color.green : Color.red;
    }

    private void Reveal()
    {
        isRevealed = true;
        if (!saveManager.data.RevealedUpgrades.Contains(upgradeSO.ID))
            saveManager.data.RevealedUpgrades.Add(upgradeSO.ID);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (isRevealed)
        {
            nameText.text = $"{upgradeSO.Name}";
            countText.text = saveManager.data.GetUpgradeCount(upgradeSO.ID).ToString();
            iconImage.sprite = upgradeSO.Icon;
            iconImage.color = Color.white;
        }
        else
        {
            nameText.text = "???";
            iconImage.sprite = upgradeSO.Icon;
            iconImage.color = Color.black;
        }
    }

    public void Buy()
    {
        if (saveManager.data.Money >= currentPrice)
        {
            saveManager.data.Money -= currentPrice;
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgradeSO.ID);
            if (state == null)
            {
                state = new UpgradeState { ID = upgradeSO.ID, Amount = 0 };
                saveManager.data.Upgrades.Add(state);
            }
            state.Amount++;
            RefreshUI();
            FindFirstObjectByType<BuildingVisualManager>().OnPurchase(upgradeSO, state.Amount);

            // После покупки вызываем проверку новых кнопок в магазине
            FindFirstObjectByType<StoreManager>().RefreshAvailableUpgrades();
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => canvasGroup.DOFade(1.0f, 0.2f);
    public void OnPointerExit(PointerEventData eventData) => canvasGroup.DOFade(0.7f, 0.2f);
}