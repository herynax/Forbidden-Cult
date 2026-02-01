using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using Lean.Localization;
using YG;

public class OfflineIncomeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private TextMeshProUGUI earnedText;

    [Header("Buttons")]
    [SerializeField] private Button claimFullWithAdButton;
    [SerializeField] private Button claimHalfButton;

    private SaveManager saveManager;
    private PassiveIncomeManager passiveManager;
    private double currentPendingEarned; // Накопленное, ожидающее решения

    private void Start()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();

        if (passiveManager != null) passiveManager.CalculateIncomeValue();

        DOVirtual.DelayedCall(0.5f, () => {
            CalculateOfflineIncome();
        }).SetUpdate(true);

        // Назначаем кнопки
        claimFullWithAdButton.onClick.AddListener(ClaimFullWithAd);
        claimHalfButton.onClick.AddListener(ClaimHalfNormal);
    }

    private void CalculateOfflineIncome()
    {
        if (saveManager == null || saveManager.data == null || passiveManager == null) return;

        passiveManager.CalculateIncomeValue();
        double cps = passiveManager.TotalIncomePerSecond;

        if (cps <= 0 || saveManager.data.LastSaveTimeTicks == 0)
        {
            saveManager.data.LastSaveTimeTicks = System.DateTime.UtcNow.Ticks;
            return;
        }

        System.DateTime lastTime = new System.DateTime(saveManager.data.LastSaveTimeTicks);
        System.TimeSpan timePassed = System.DateTime.UtcNow - lastTime;

        if (timePassed.TotalSeconds < 10) return;

        // Считаем ПОЛНУЮ сумму, но пока НЕ начисляем её в SaveManager
        currentPendingEarned = cps * timePassed.TotalSeconds;

        if (currentPendingEarned > 0.01)
        {
            ShowWelcomePanel(currentPendingEarned, timePassed);
        }

        saveManager.data.LastSaveTimeTicks = System.DateTime.UtcNow.Ticks;
    }

    private void ShowWelcomePanel(double amount, System.TimeSpan span)
    {
        welcomePanel.SetActive(true);

        string h = LeanLocalization.GetTranslationText("Time_HourShort");
        string m = LeanLocalization.GetTranslationText("Time_MinShort");
        string s = LeanLocalization.GetTranslationText("Time_SecShort");

        string welcomeHeader = LeanLocalization.GetTranslationText("UI_WelcomeBack");
        string infoFull = LeanLocalization.GetTranslationText("UI_Offline_Info_Full");

        string timeStr = $"{span.Hours}{h} {span.Minutes}{m} {span.Seconds}{s}";

        // Показываем игроку, сколько он МОЖЕТ заработать
        earnedText.text = $"{welcomeHeader}\n <color=#B000FF>{timeStr}</color>.\n{string.Format(infoFull, BigNumberFormatter.Format(amount))}";

        welcomePanel.transform.DOKill();
        welcomePanel.transform.localScale = Vector3.zero;
        welcomePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    // ВАРИАНТ 1: Реклама (100%)
    private void ClaimFullWithAd()
    {
        YG2.RewardedAdvShow("offline_full", () =>
        {
            saveManager.data.Money += currentPendingEarned;
            saveManager.Save();
            ClosePanel();
        });
    }

    // ВАРИАНТ 2: Без рекламы (50%)
    private void ClaimHalfNormal()
    {
        saveManager.data.Money += (currentPendingEarned / 2.0);
        saveManager.Save();
        ClosePanel();
    }

    private void ClosePanel()
    {
        welcomePanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
            welcomePanel.SetActive(false);
        });
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        AudioListener.pause = !hasFocus;
        if (!hasFocus && saveManager != null) saveManager.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        AudioListener.pause = pauseStatus;
        if (pauseStatus && saveManager != null) saveManager.Save();
    }
}