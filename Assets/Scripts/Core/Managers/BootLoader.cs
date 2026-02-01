using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FMODUnity;
using YG;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private float timeout = 10f; // Тайм-аут на случай ошибки

    private IEnumerator Start()
    {
        Debug.Log("BOOT: Начинаю загрузку...");

        // 1. Ожидание SDK Яндекса
        float timer = 0;
        while (!YG2.isSDKEnabled)
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogError("BOOT ERROR: Яндекс SDK не ответил вовремя!");
                break;
            }
            yield return null;
        }
        Debug.Log("BOOT: YG2 SDK готов.");

        // 2. Ожидание инициализации FMOD
        while (!RuntimeManager.IsInitialized)
        {
            yield return null;
        }
        Debug.Log("BOOT: FMOD инициализирован.");

        // 3. Ожидание загрузки аудио-банков
        // Если банков нет или они не настроены, этот цикл может длиться вечно
        while (!RuntimeManager.HaveAllBanksLoaded)
        {
            yield return null;
        }
        Debug.Log("BOOT: Все аудио-банки загружены.");

        // Даем маааленькую паузу для стабильности
        yield return new WaitForSeconds(0.5f);

        // Сообщаем Яндексу, что игра готова (важно для метрик)
        YG2.GameReadyAPI();
        Debug.Log($"BOOT: Загружаю сцену {nextSceneName}...");

        // Используем наш SceneLoader если он есть, иначе обычный SceneManager
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}