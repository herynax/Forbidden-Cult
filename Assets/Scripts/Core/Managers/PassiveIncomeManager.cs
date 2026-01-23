using UnityEngine;
using TMPro;

public class PassiveIncomeManager : MonoBehaviour
{
    public SaveManager saveManager;
    public UpgradeSO[] allUpgrades;
    public TextMeshProUGUI totalIncomeText;

    private double totalIncome;

    private void Start()
    {
        InvokeRepeating(nameof(AddIncome), 1f, 1f);
    }

    private void Update()
    {
        CalculateIncome();

        // Защита: если ты забыл перетащить TextMeshPro в инспектор
        if (totalIncomeText != null)
        {
            totalIncomeText.text = totalIncome.ToString("F1");
        }
    }

    void CalculateIncome()
    {
        totalIncome = 0;

        BuildingEntity[] allBuildings = Object.FindObjectsByType<BuildingEntity>(FindObjectsSortMode.None);

        if (allBuildings == null) return;

        foreach (var b in allBuildings)
        {
            // Проверяем b на null (на случай удаления объекта в этом же кадре)
            if (b != null && b.currentState == BuildingEntity.State.Active)
            {
                totalIncome += b.GetPassiveIncome();
            }
        }
    }

    void AddIncome()
    {
        saveManager.data.Money += totalIncome;

        foreach (var upgrade in allUpgrades)
        {
            if (saveManager.data.GetUpgradeCount(upgrade.ID) > 0)
            {
                // Используем новый метод поиска и здесь для консистентности
                var bvm = Object.FindFirstObjectByType<BuildingVisualManager>();
                if (bvm != null) bvm.PulseRow(upgrade.ID);
            }
        }
    }
}