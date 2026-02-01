using System.Collections.Generic;
using UnityEngine;

namespace YG
{
    // partial позволяет "дописывать" класс SavesYG, который уже есть в плагине
    public partial class SavesYG
    {
        // Поля данных
        public double Money = 0;
        public double ClickPower = 1;
        public bool languageChosen = false;
        public long LastSaveTimeTicks = 0;
        public double MoneyAtLeave = -1;

        public List<UpgradeState> Upgrades = new List<UpgradeState>();
        public List<string> RevealedUpgrades = new List<string>();
        public List<MiniGameProgress> MiniGames = new List<MiniGameProgress>();

        // --- ВОЗВРАЩАЕМ МЕТОДЫ-ПОМОЩНИКИ ---
        public int GetUpgradeCount(string id)
        {
            // Проверяем на null на всякий случай
            if (Upgrades == null) Upgrades = new List<UpgradeState>();

            var upgrade = Upgrades.Find(u => u.ID == id);
            return upgrade != null ? upgrade.Amount : 0;
        }

        public bool IsRevealed(string id)
        {
            if (RevealedUpgrades == null) RevealedUpgrades = new List<string>();
            return RevealedUpgrades.Contains(id);
        }
    }

    [System.Serializable]
    public class UpgradeState
    {
        public string ID;
        public int Amount;
        public double TotalEarned;
        public List<VisualPosition> StoredPositions = new List<VisualPosition>();
    }

    [System.Serializable]
    public class VisualPosition
    {
        public float x, y, zRotation;
        public float r, g, b, a;

        public VisualPosition(float x, float y, float zRotation, Color color)
        {
            this.x = x; this.y = y; this.zRotation = zRotation;
            this.r = color.r; this.g = color.g; this.b = color.b; this.a = color.a;
        }
        public Color GetColor() => new Color(r, g, b, a);
    }

    [System.Serializable]
    public class MiniGameProgress
    {
        public string TypeID;
        public int CurrentLevelIndex = 0;
    }
}