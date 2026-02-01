using FMODUnity;
using Lean.Localization;
using UnityEngine;
using YG;

public class YandexManager : MonoBehaviour
{
    public static YandexManager Instance;

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
        YG2.onOpenInterAdv += MuteAudio;
        YG2.onCloseInterAdv += UnmuteAudio;
        YG2.onGetSDKData += OnDataLoaded;

        // ???????? ?? ??????????? (???? ??????????? Lean)
        YG2.onCorrectLang += SyncLanguage;
    }

    private void OnDisable()
    {
        YG2.onOpenInterAdv -= MuteAudio;
        YG2.onCloseInterAdv -= UnmuteAudio;
        YG2.onCorrectLang -= SyncLanguage;
        YG2.onGetSDKData -= OnDataLoaded;
    }

    private void Start()
    {
        if (YG2.isSDKEnabled)
        {
            string language = YG2.envir.language;
            SyncLanguage(language);
        }
    }

    private void SyncLanguage(string lang)
    {
        // ?????????? ??? ?? env (EnvirData)
        Debug.Log("YG2 EnvirData Language: " + lang);

        string target = lang switch
        {
            "ru" => "Russian",
            "en" => "English",
            _ => "Russian"
        };
        LeanLocalization.SetCurrentLanguageAll(target);
    }

    private void MuteAudio()
    {
        SetFMODSystemState(true);
        Time.timeScale = 0;
    }

    private void UnmuteAudio()
    {
        SetFMODSystemState(false);
    }

    // ??????? ????? ??? ????????????
    private void OnApplicationFocus(bool hasFocus)
    {
        // ???? ???????? ????? - ?????? ?????? ?????????
        if (!hasFocus)
        {
            SetFMODSystemState(true);
        }
        else
        {
            // ?????????? ????, ?????? ???? ??????? ?? ???????? ????? ??????
            if (!YG2.nowInterAdv && !YG2.nowRewardAdv)
            {
                SetFMODSystemState(false);
            }
        }
    }

    private void OnDataLoaded()
    {
        Debug.Log("Cloud data received!");

        SyncLanguage(YG2.envir.language);

        var offline = Object.FindFirstObjectByType<OfflineIncomeManager>();
        if (offline != null) offline.enabled = true;
    }

    private void SetFMODSystemState(bool mute)
    {
        // ????????: ???? FMOD ??? ?? ???????????????, ?? ??????? ???
        if (!FMODUnity.RuntimeManager.IsInitialized) return;

        try
        {
            FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");
            if (masterBus.isValid())
            {
                masterBus.setMute(mute);
            }

            if (mute)
                RuntimeManager.CoreSystem.mixerSuspend();
            else
                RuntimeManager.CoreSystem.mixerResume();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FMOD not ready yet: " + e.Message);
        }

        AudioListener.pause = mute;
    }
}