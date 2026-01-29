using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using Lean.Localization;

public class OfflineIncomeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private TextMeshProUGUI earnedText;
    [SerializeField] private Button closeButton;

    private SaveManager saveManager;
    private PassiveIncomeManager passiveManager;

    private void Start()
    {
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();

        // ВАЖНО: сначала просим менеджер дохода обновить цифру CPS
        // иначе passiveManager.TotalIncomePerSecond будет равен 0 в первый кадр
        if (passiveManager != null) passiveManager.CalculateIncomeValue();

        // Запускаем расчет с микро-задержкой
        DOVirtual.DelayedCall(0.2f, () => {
            CalculateOfflineIncome();
        }).SetUpdate(true);

        closeButton.onClick.AddListener(ClosePanel);
    }

    private void CalculateOfflineIncome()
    {
        if (saveManager == null || saveManager.data == null || passiveManager == null) return;

        double earned = 0;
        System.TimeSpan timePassed = System.TimeSpan.Zero;
        bool fromMinigame = false;

        // 1. ПРОВЕРКА: Если мы вернулись из мини-игры (MoneyAtLeave > 0)
        if (saveManager.data.MoneyAtLeave > 0)
        {
            earned = saveManager.data.Money - saveManager.data.MoneyAtLeave;
            fromMinigame = true;

            // Сбрасываем метку ухода
            saveManager.data.MoneyAtLeave = -1;
        }
        // 2. ПРОВЕРКА: Если это был холодный запуск (офлайн через время)
        else if (saveManager.data.LastSaveTimeTicks > 0)
        {
            System.DateTime lastTime = new System.DateTime(saveManager.data.LastSaveTimeTicks);
            timePassed = System.DateTime.UtcNow - lastTime;

            if (timePassed.TotalSeconds >= 10)
            {
                passiveManager.CalculateIncomeValue();
                earned = passiveManager.TotalIncomePerSecond * timePassed.TotalSeconds;
                saveManager.data.Money += earned;
            }
        }

        // Если ничего не куплено — просто обновляем время и выходим
        if (passiveManager.TotalIncomePerSecond <= 0 && !fromMinigame)
        {
            saveManager.data.LastSaveTimeTicks = System.DateTime.UtcNow.Ticks;
            return;
        }

        // Если заработано достаточно — показываем панель
        if (earned > 0.01)
        {
            ShowWelcomePanel(earned, timePassed);
        }

        // Обновляем метку времени для следующего раза
        saveManager.data.LastSaveTimeTicks = System.DateTime.UtcNow.Ticks;
    }

    private void ShowWelcomePanel(double amount, System.TimeSpan span)
    {
        welcomePanel.SetActive(true);

        // Получаем переведенные слова для времени
        string h = LeanLocalization.GetTranslationText("Time_HourShort"); // "ч" или "h"
        string m = LeanLocalization.GetTranslationText("Time_MinShort");  // "м" или "m"
        string s = LeanLocalization.GetTranslationText("Time_SecShort");  // "с" или "s"

        string welcomeHeader = LeanLocalization.GetTranslationText("UI_WelcomeBack"); // "Вас не было"
        string gatheredText = LeanLocalization.GetTranslationText("UI_Gathered"); // "Культисты собрали"

        // Формируем строку времени: "02ч 15м 05с"
        string timeStr = $"{span.Hours}{h} {span.Minutes}{m} {span.Seconds}{s}";

        // Финальная сборка
        earnedText.text = $"{welcomeHeader}\n <color=#B000FF>{timeStr}</color>.\n{gatheredText}\n <color=#B000FF>{BigNumberFormatter.Format(amount)}</color>!";

        welcomePanel.transform.DOKill();
        welcomePanel.transform.localScale = Vector3.zero;
        welcomePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
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