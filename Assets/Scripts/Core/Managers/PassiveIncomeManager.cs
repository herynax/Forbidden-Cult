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
        // 1. Пересчитываем текущий доход (можно делать реже для оптимизации, но для простоты здесь так)
        CalculateIncomeValue();

        // 2. Начисляем доход ПЛАВНО. 
        // Мы берем доход в секунду и делим его на количество кадров (Time.deltaTime)
        double incomeThisFrame = totalIncomePerSecond * Time.deltaTime;
        saveManager.data.Money += incomeThisFrame;

        // 3. Обновляем визуализацию
        if (moneyDisplay != null)
            moneyDisplay.text = BigNumberFormatter.Format(saveManager.data.Money);

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