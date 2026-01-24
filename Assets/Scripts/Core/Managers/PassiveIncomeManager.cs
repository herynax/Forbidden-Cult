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
        BuildingEntity[] allBuildings = Object.FindObjectsByType<BuildingEntity>(FindObjectsSortMode.None);
        foreach (var b in allBuildings)
        {
            if (b != null && b.currentState == BuildingEntity.State.Active)
            {
                totalIncomePerSecond += b.GetPassiveIncome();
            }
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