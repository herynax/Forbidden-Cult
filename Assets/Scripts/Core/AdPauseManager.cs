using UnityEngine;
using YG;
using FMODUnity;
using DG.Tweening;

public class AdPauseManager : MonoBehaviour
{
    public static AdPauseManager Instance;

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

    private void OnEnable()
    {
        // Подписываемся на события открытия и закрытия любой рекламы
        YG2.onOpenAnyAdv += PauseGame;
        YG2.onCloseAnyAdv += UnpauseGame;

        YG2.onErrorInterAdv += UnpauseGame;

        YG2.onErrorRewardedAdv += UnpauseGame;
    }

    private void OnDisable()
    {
        YG2.onOpenAnyAdv -= PauseGame;
        YG2.onCloseAnyAdv -= UnpauseGame;
        YG2.onErrorInterAdv -= UnpauseGame;

        // Не забываем отписаться
        YG2.onErrorRewardedAdv -= UnpauseGame;
    }

    private void PauseGame()
    {
        Debug.Log("AD: Пауза игры для рекламы");

        // 1. Останавливаем время (прекращается расчет Time.deltaTime в PassiveIncomeManager)
        Time.timeScale = 0;

        // 2. Ставим DOTween на паузу (чтобы анимации не дергались под рекламой)
        DOTween.PauseAll();

        // 3. Глушим FMOD
        SetFMODState(true);
    }

    private void UnpauseGame()
    {
        Debug.Log("AD: Реклама закрыта, возвращаемся");

        // 1. Возвращаем время
        Time.timeScale = 1;

        // 2. Запускаем анимации
        DOTween.PlayAll();

        // 3. Включаем FMOD
        SetFMODState(false);
    }

    private void SetFMODState(bool mute)
    {
        if (!RuntimeManager.IsInitialized) return;

        try
        {
            // Получаем мастер-шину и мутим её
            FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");
            if (masterBus.isValid())
            {
                masterBus.setMute(mute);
            }

            // Для WebGL критически важно вызывать приостановку микшера
            if (mute)
                RuntimeManager.CoreSystem.mixerSuspend();
            else
                RuntimeManager.CoreSystem.mixerResume();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FMOD Ad Pause Error: " + e.Message);
        }
    }
}