using UnityEngine;

public class VFXController : MonoBehaviour
{
    [SerializeField] private ParticleSystem targetSystem;
    [SerializeField] private int particlesPerBuilding = 1; // Сколько лимита добавляет одно здание
    [SerializeField] private int baseMaxParticles = 100;    // Начальный лимит

    private PassiveIncomeManager passiveManager;

    private void Start()
    {
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
    }

    private void Update()
    {
        if (targetSystem == null || passiveManager == null) return;

        // 1. Получаем текущее количество фактически заспавненных зданий
        int buildingCount = GetCurrentBuildingCount();

        // 2. Рассчитываем новый лимит
        int newMax = baseMaxParticles + (buildingCount * particlesPerBuilding);

        // 3. Обращаемся к модулю Main для изменения параметров
        var main = targetSystem.main;

        // Меняем значение только если оно отличается (для оптимизации)
        if (main.maxParticles != newMax)
        {
            main.maxParticles = newMax;
        }
    }

    private int GetCurrentBuildingCount()
    {
        // Можно считать через список, который мы сделали в прошлом шаге
        // (Убедись, что список в PassiveIncomeManager публичный или есть метод)
        // return passiveManager.activeBuildingInstances.Count; 

        // Либо быстрый способ найти все BuildingEntity на сцене:
        return Object.FindObjectsByType<BuildingEntity>(FindObjectsSortMode.None).Length;
    }
}