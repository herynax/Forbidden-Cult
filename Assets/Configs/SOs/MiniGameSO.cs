using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMiniGame", menuName = "Clicker/MiniGame")]
public class MiniGameSO : ScriptableObject
{
    public string TypeID;         // Уникальный ID (например, "Arena")
    public string SceneName;      // Имя сцены мини-игры
    public List<string> RequiredUpgradeIDs; // Список ID зданий для открытия

    [Header("Spawn Timers")]
    public float minCooldown = 60f; // Рандом от 1 мин
    public float maxCooldown = 180f; // До 3 мин
}