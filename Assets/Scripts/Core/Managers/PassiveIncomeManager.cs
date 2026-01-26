using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PassiveIncomeManager : MonoBehaviour
{
    private SaveManager saveManager;
    public UpgradeSO[] allUpgrades;
    public TextMeshProUGUI totalIncomeText;
    public TextMeshProUGUI moneyDisplay;

    private List<BuildingEntity> activeBuildingInstances = new List<BuildingEntity>();


    private double totalIncomePerSecond;

    public double TotalIncomePerSecond => totalIncomePerSecond;

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    // Методы для регистрации иконок (вызываются из BuildingEntity)
    public void RegisterBuilding(BuildingEntity building) => activeBuildingInstances.Add(building);
    public void UnregisterBuilding(BuildingEntity building) => activeBuildingInstances.Remove(building);

    private void Update()
    {
        if (saveManager == null || saveManager.data == null) return;

        CalculateIncomeValue();

        // Добавляем доход в секунду для каждого типа зданий (статистика)
        foreach (var upgSO in allUpgrades)
        {
            if (upgSO == null) continue;
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgSO.ID);
            if (state == null || state.Amount <= 0) continue;

            double typeCPS = state.Amount * upgSO.BasePassiveIncome;

            // Штраф за спящих (только если мы в сцене, где они физически есть)
            foreach (var instance in activeBuildingInstances)
            {
                if (instance != null && instance.GetUpgradeID() == upgSO.ID && instance.currentState == BuildingEntity.State.Sleeping)
                {
                    typeCPS -= upgSO.BasePassiveIncome;
                }
            }
            if (typeCPS < 0) typeCPS = 0;

            state.TotalEarned += typeCPS * Time.deltaTime;
        }

        // Общий доход
        double incomeThisFrame = totalIncomePerSecond * Time.deltaTime;
        saveManager.data.Money += incomeThisFrame;

        // ВАЖНО: Обновляем тексты ТОЛЬКО если они назначены (их не будет в мини-играх)
        if (moneyDisplay != null)
            moneyDisplay.text = BigNumberFormatter.Format(saveManager.data.Money);

        if (totalIncomeText != null)
            totalIncomeText.text = BigNumberFormatter.Format(totalIncomePerSecond);
    }

    public void CalculateIncomeValue()
    {
        if (saveManager == null || saveManager.data == null) return;

        // 1. Считаем полный потенциальный доход из сохранений
        double potentialIncome = 0;
        foreach (var upg in allUpgrades)
        {
            if (upg == null) continue;
            int count = saveManager.data.GetUpgradeCount(upg.ID);
            potentialIncome += count * upg.BasePassiveIncome;
        }

        // 2. Считаем штраф за "спящие" объекты в текущей сцене
        double sleepPenalty = 0;
        for (int i = activeBuildingInstances.Count - 1; i >= 0; i--)
        {
            var b = activeBuildingInstances[i];
            if (b == null) { activeBuildingInstances.RemoveAt(i); continue; }

            // Если здание в сцене спит — вычитаем его доход из общего пула
            if (b.currentState == BuildingEntity.State.Sleeping)
            {
                sleepPenalty += b.GetIncomeValue();
            }
        }

        // Итоговый доход = Всё что куплено МИНУС то что сейчас спит
        totalIncomePerSecond = potentialIncome - sleepPenalty;

        // Защита от отрицательного дохода (на всякий случай)
        if (totalIncomePerSecond < 0) totalIncomePerSecond = 0;
    }


    // Раз в секунду можно пушить визуальный эффект PulseRow, чтобы не спамить в Update
    private float pulseTimer;
    private void FixedUpdate()
    {
        pulseTimer += Time.fixedDeltaTime;
        if (pulseTimer >= 1f)
        {
            pulseTimer = 0;
            TriggerPulse();
        }
    }

    void TriggerPulse()
    {
        var bvm = Object.FindFirstObjectByType<BuildingVisualManager>();
        if (bvm == null) return;

        foreach (var upgrade in allUpgrades)
        {
            if (saveManager.data.GetUpgradeCount(upgrade.ID) > 0)
            {
                bvm.PulseRow(upgrade.ID);
            }
        }
    }
}