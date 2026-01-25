using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FMODUnity;

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

    private void Awake()
    {
        // Делаем менеджер настроек вечным, чтобы он один раз загрузил всё при старте
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Инициализируем шины
        musicBus = RuntimeManager.GetBus(musicBusPath);
        sfxBus = RuntimeManager.GetBus(sfxBusPath);

        // 1. ЗАГРУЖАЕМ НАСТРОЙКИ ИЗ ПАМЯТИ
        LoadAndApplySettings();
    }

    private void Start()
    {
        // Настройка анимации
        openedPos = settingsPanel.anchoredPosition;
        closedPos = settingsButton.GetComponent<RectTransform>().anchoredPosition;

        settingsPanel.anchoredPosition = closedPos;
        settingsPanel.localScale = Vector3.zero;
        panelAlpha.alpha = 0;
        settingsPanel.gameObject.SetActive(false);

        // Подписываемся на события слайдеров
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        globalParamSlider.onValueChanged.AddListener(SetGlobalParameter);
    }

    private void LoadAndApplySettings()
    {
        // Достаем значения (если их нет, ставим 0.75f по умолчанию)
        float mVol = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float sVol = PlayerPrefs.GetFloat("SFXVol", 0.75f);
        float gParam = PlayerPrefs.GetFloat("GlobalParam", 0.5f);

        // Применяем к слайдерам (это вызовет методы Set...Volume автоматически через Listener)
        musicSlider.value = mVol;
        sfxSlider.value = sVol;
        globalParamSlider.value = gParam;

        // Принудительно применяем к FMOD на старте
        musicBus.setVolume(mVol);
        sfxBus.setVolume(sVol);
        RuntimeManager.StudioSystem.setParameterByName(globalParamName, gParam);
    }

    public void ToggleSettings()
    {
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
            // Сохраняем в память при закрытии панели (на всякий случай)
            PlayerPrefs.Save();

            settingsPanel.DOAnchorPos(closedPos, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
            settingsPanel.DOScale(0f, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
            panelAlpha.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() => settingsPanel.gameObject.SetActive(false));
        }
    }

    // МЕТОДЫ ИЗМЕНЕНИЯ (Вызываются слайдерами)

    private void SetMusicVolume(float value)
    {
        musicBus.setVolume(value);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    private void SetSFXVolume(float value)
    {
        sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    private void SetGlobalParameter(float value)
    {
        RuntimeManager.StudioSystem.setParameterByName(globalParamName, value);
        PlayerPrefs.SetFloat("GlobalParam", value);
    }
}