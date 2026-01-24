using UnityEngine;
using TMPro;

public class PassiveIncomeManager : MonoBehaviour
{
    private SaveManager saveManager;
    public UpgradeSO[] allUpgrades;
    public TextMeshProUGUI totalIncomeText;
    public TextMeshProUGUI moneyDisplay; // Ссылка на главный текст денег

    private double totalIncomePerSecond;

    public double TotalIncomePerSecond => totalIncomePerSecond;

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    private void Update()
    {
        if (saveManager == null || saveManager.data == null) return;

        double frameTotalIncome = 0;

        // Проходим по всем типам зданий, которые есть в игре
        foreach (var upgradeSO in allUpgrades)
        {
            if (upgradeSO == null) continue;

            // Ищем состояние этого здания в сохранениях
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgradeSO.ID);

            if (state != null && state.Amount > 0)
            {
                // Считаем, сколько этот тип зданий приносит в секунду
                double incomePerSecondForThisType = state.Amount * upgradeSO.BasePassiveIncome;

                // Считаем, сколько привалило за этот конкретный кадр
                double frameIncomeForThisType = incomePerSecondForThisType * Time.deltaTime;

                // ВАЖНО: Добавляем этот доход в личную статистику здания!
                // Именно эту переменную читает TooltipManager
                state.TotalEarned += frameIncomeForThisType;

                // Суммируем для общего баланса
                frameTotalIncome += incomePerSecondForThisType;
            }
        }

        // Обновляем общую сумму дохода в секунду для отображения в UI
        totalIncomePerSecond = frameTotalIncome;

        // Начисляем деньги в общий банк игрока
        saveManager.data.Money += totalIncomePerSecond * Time.deltaTime;

        // Обновляем главный текст денег
        if (moneyDisplay != null)
            moneyDisplay.text = BigNumberFormatter.Format(saveManager.data.Money);

        // Обновляем текст дохода в секунду
        if (totalIncomeText != null)
            totalIncomeText.text = BigNumberFormatter.Format(totalIncomePerSecond);
    }

    void CalculateIncomeValue()
    {
        totalIncomePerSecond = 0;

        // Проверка на наличие менеджера и данных
        if (saveManager == null || saveManager.data == null) return;

        // Проходим по всем типам улучшений, которые мы закинули в массив в инспекторе
        foreach (var upgradeSO in allUpgrades)
        {
            if (upgradeSO == null) continue;

            // Берем количество купленных зданий этого типа из сохранения
            int count = saveManager.data.GetUpgradeCount(upgradeSO.ID);

            // Прибавляем к общему доходу: (Количество * доход одного здания)
            totalIncomePerSecond += count * upgradeSO.BasePassiveIncome;
        }
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