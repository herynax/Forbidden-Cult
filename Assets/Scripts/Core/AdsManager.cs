using UnityEngine;
using YG;
using Lean.Localization;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("Settings")]
    [SerializeField] private float interAdInterval = 60f;
    private float interTimer;

    private SaveManager saveManager;
    private PassiveIncomeManager passiveManager;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
        interTimer = interAdInterval;
    }

    private void Update()
    {
        // Таймер для Interstitial Ad
        if (interTimer > 0)
        {
            interTimer -= Time.deltaTime;
        }
        else
        {
            if (YG2.isSDKEnabled && !YG2.nowAdsShow)
            {
                YG2.InterstitialAdvShow();
                interTimer = interAdInterval;
            }
        }
    }

    // Метод для вызова из UI (кнопка в магазине или на экране)
    public void ShowRewarded90sBonus()
    {
        YG2.RewardedAdvShow("bonus_90s", () =>
        {
            if (passiveManager != null && saveManager != null)
            {
                double reward = passiveManager.TotalIncomePerSecond * 90f;
                saveManager.data.Money += reward;
                saveManager.Save();

                Debug.Log("Получен бонус за 90 секунд!");
            }
        });
    }
}