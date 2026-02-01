using UnityEngine;
using YG;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    // Свойство для быстрого доступа к данным
    public SavesYG data => YG2.saves;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Save()
    {
        // Фиксируем время перед отправкой в облако
        data.LastSaveTimeTicks = System.DateTime.UtcNow.Ticks;

        // Метод плагина для сохранения (локально + облако)
        YG2.SaveProgress();
        Debug.Log("Game Saved to Yandex Cloud");
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        // В YG2 сброс делается через новый экземпляр
        YG2.saves = new SavesYG();
        Save();
        // Перезагружаем сцену, чтобы визуал обновился
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}