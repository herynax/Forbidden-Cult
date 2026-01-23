using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public GameData data;
    private string saveKey = "PlayerSave";

    private void Awake()
    {
        Load();
        StartCoroutine(FindFirstObjectByType<BuildingVisualManager>().RestoreVisuals());
    }

    public void Save()
    {
        // Превращаем объект в строку
        string json = JsonUtility.ToJson(data);
        // Сохраняем в PlayerPrefs (в Вебе это IndexedDB)
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        Debug.Log("Saved: " + json);
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            data = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            data = new GameData(); // Если сохранения нет, создаем новое
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        data = new GameData();
        Save();
    }
}