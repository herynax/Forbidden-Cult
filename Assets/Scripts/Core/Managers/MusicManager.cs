using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [Header("FMOD Settings")]
    public EventReference musicEventPath;

    [Header("Bank Loading")]
    public List<string> banksToLoad = new List<string> { "Master", "Master.strings", "Music" };

    private EventInstance musicInstance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadFMODBanks());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator LoadFMODBanks()
    {
        foreach (string bankName in banksToLoad)
        {
            RuntimeManager.LoadBank(bankName, true);
        }

        while (!RuntimeManager.HaveAllBanksLoaded)
        {
            yield return null;
        }

        yield return null;
        StartMusic();
    }

    private void StartMusic()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicInstance.start();
    }

    private void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
    }
}