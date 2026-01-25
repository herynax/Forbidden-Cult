using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UpgradeButton upgradeButton;
    private SaveManager saveManager;

    private void Awake()
    {
        upgradeButton = GetComponent<UpgradeButton>();
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (upgradeButton == null || upgradeButton.upgradeSO == null) return;

        // Берем количество из сейва
        int count = saveManager.data.GetUpgradeCount(upgradeButton.upgradeSO.ID);

        // Вызываем показ (менеджер сам проверит, раскрыто ли оно через saveManager.data.RevealedUpgrades)
        TooltipManager.Instance.ShowTooltip(upgradeButton.upgradeSO, count);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}