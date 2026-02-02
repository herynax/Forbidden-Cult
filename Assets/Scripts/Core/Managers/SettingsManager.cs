using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FMODUnity;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("UI Containers")]
    [SerializeField] private RectTransform settingsPanel;
    [SerializeField] private CanvasGroup panelAlpha;
    [SerializeField] private Button settingsButton;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider globalParamSlider;

    [Header("FMOD Paths")]
    [SerializeField] private string musicBusPath = "bus:/Music";
    [SerializeField] private string sfxBusPath = "bus:/SFX";
    [SerializeField] private string globalParamName = "ClickIntensity";

    private FMOD.Studio.Bus musicBus;
    private FMOD.Studio.Bus sfxBus;

    private bool isOpen = false;
    private Vector2 closedPos;
    private Vector2 openedPos;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ВАЖНО: Если этот объект — часть UI на Канвасе, 
            // DontDestroyOnLoad может работать криво вместе с родительским Канвасом.
            // Убедись, что SettingsManager висит на корневом объекте, а не внутри UI.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator Start()
    {
        // Скрываем панель сразу (визуально)
        SetupInitialUI();

        // ЖДЕМ ИНИЦИАЛИЗАЦИИ FMOD (Критично для Веба)
        while (!RuntimeManager.IsInitialized) yield return null;
        while (!RuntimeManager.HaveAllBanksLoaded) yield return null;

        // Получаем шины только когда всё загружено
        musicBus = RuntimeManager.GetBus(musicBusPath);
        sfxBus = RuntimeManager.GetBus(sfxBusPath);

        // Загружаем и применяем настройки
        LoadAndApplySettings();

        // Подписываемся на события
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        globalParamSlider.onValueChanged.AddListener(SetGlobalParameter);

        isInitialized = true;
    }

    private void SetupInitialUI()
    {
        openedPos = settingsPanel.anchoredPosition;
        closedPos = settingsButton.GetComponent<RectTransform>().anchoredPosition;

        settingsPanel.anchoredPosition = closedPos;
        settingsPanel.localScale = Vector3.zero;
        panelAlpha.alpha = 0;
        settingsPanel.gameObject.SetActive(false);
    }

    private void LoadAndApplySettings()
    {
        float mVol = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float sVol = PlayerPrefs.GetFloat("SFXVol", 0.75f);
        float gParam = PlayerPrefs.GetFloat("GlobalParam", 0.5f);

        musicSlider.value = mVol;
        sfxSlider.value = sVol;
        globalParamSlider.value = gParam;

        // Применяем принудительно
        ApplyVolumes(mVol, sVol, gParam);
    }

    private void ApplyVolumes(float m, float s, float g)
    {
        if (musicBus.isValid()) musicBus.setVolume(m);
        if (sfxBus.isValid()) sfxBus.setVolume(s);
        RuntimeManager.StudioSystem.setParameterByName(globalParamName, g);
    }

    public void ToggleSettings()
    {
        if (!isInitialized) return; // Не даем открывать, пока FMOD не готов

        isOpen = !isOpen;
        settingsPanel.DOKill();
        panelAlpha.DOKill();

        if (isOpen)
        {
            settingsPanel.gameObject.SetActive(true);
            settingsPanel.DOAnchorPos(openedPos, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
            settingsPanel.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
            panelAlpha.DOFade(1f, 0.3f).SetUpdate(true);
        }
        else
        {
            PlayerPrefs.Save();
            settingsPanel.DOAnchorPos(closedPos, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
            settingsPanel.DOScale(0f, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
            panelAlpha.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() => settingsPanel.gameObject.SetActive(false));
        }
    }

    private void SetMusicVolume(float value)
    {
        if (musicBus.isValid()) musicBus.setVolume(value);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    private void SetSFXVolume(float value)
    {
        if (sfxBus.isValid()) sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    private void SetGlobalParameter(float value)
    {
        RuntimeManager.StudioSystem.setParameterByName(globalParamName, value);
        PlayerPrefs.SetFloat("GlobalParam", value);
    }
}