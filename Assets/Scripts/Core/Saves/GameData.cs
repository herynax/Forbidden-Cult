using System.Collections.Generic;


[System.Serializable]
public class VisualPosition
{
    public float x;
    public float y;
    public float zRotation;

    public VisualPosition(float x, float y, float zRotation)
    {
        this.x = x;
        this.y = y;
        this.zRotation = zRotation;
    }
}

[System.Serializable]
public class UpgradeState
{
    public string ID;
    public int Amount;
    public double TotalEarned; // Сколько скверны принесло именно это улучшение за всё время
    public List<VisualPosition> StoredPositions = new List<VisualPosition>();
}

[System.Serializable]
public class MiniGameProgress
{
    public string TypeID;
    public int CurrentLevelIndex = 0;
    public bool IsButtonActive = false; // Ждет ли кнопка игрока
}

[System.Serializable]
public class GameData
{
    public double Money = 0;
    public float ClickPower = 1;
    
    public List<UpgradeState> Upgrades = new List<UpgradeState>();
    public List<string> RevealedUpgrades = new List<string>(); // Список раскрытых ID

    public bool IsRevealed(string id) => RevealedUpgrades.Contains(id);


    public List<MiniGameProgress> MiniGames = new List<MiniGameProgress>();

    public MiniGameProgress GetProgress(string typeID)
    {
        var p = MiniGames.Find(x => x.TypeID == typeID);
        if (p == null)
        {
            p = new MiniGameProgress { TypeID = typeID };
            MiniGames.Add(p);
        }
        return p;
    }

    public int GetUpgradeCount(string id)
    {
        var upgrade = Upgrades.Find(u => u.ID == id);
        return upgrade != null ? upgrade.Amount : 0;
    }
}