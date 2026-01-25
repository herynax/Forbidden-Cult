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

        double frameTotalCPS = 0;

        // Проходим по всем типам улучшений
        foreach (var upgSO in allUpgrades)
        {
            if (upgSO == null) continue;

            // Ищем данные о покупках этого типа
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgSO.ID);
            if (state == null || state.Amount <= 0) continue;

            // 1. Считаем базовый доход этого типа (количество * доход одного)
            double typeCPS = state.Amount * upgSO.BasePassiveIncome;

            // 2. Вычитаем штраф за спящих именно этого типа
            foreach (var instance in activeBuildingInstances)
            {
                // Проверяем, что это здание того же типа и оно спит
                if (instance != null && instance.GetUpgradeID() == upgSO.ID && instance.currentState == BuildingEntity.State.Sleeping)
                {
                    typeCPS -= upgSO.BasePassiveIncome;
                }
            }

            if (typeCPS < 0) typeCPS = 0;

            // 3. Добавляем доход в личную статистику этого здания (для тултипа)
            state.TotalEarned += typeCPS * Time.deltaTime;

            // 4. Добавляем в общую сумму для кошелька
            frameTotalCPS += typeCPS;
        }

        totalIncomePerSecond = frameTotalCPS;
        saveManager.data.Money += totalIncomePerSecond * Time.deltaTime;

        // Обновление текстов
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