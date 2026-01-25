using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

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
        if (saveManager.data.LastSaveTimeTicks <= 0) return;

        System.DateTime lastTime = new System.DateTime(saveManager.data.LastSaveTimeTicks);
        System.DateTime currentTime = System.DateTime.UtcNow;
        System.TimeSpan timePassed = currentTime - lastTime;

        double secondsOffline = timePassed.TotalSeconds;

        // Снижаем порог до 1 секунды, чтобы мини-игры тоже считались
        if (secondsOffline < 1) return;

        double cps = passiveManager.TotalIncomePerSecond;
        double earned = cps * secondsOffline;

        if (earned > 0.01)
        {
            // Начисляем доход за время мини-игры
            saveManager.data.Money += earned;
            ShowWelcomePanel(earned, timePassed);
        }
    }

    private void ShowWelcomePanel(double amount, System.TimeSpan span)
    {
        welcomePanel.SetActive(true);
        welcomePanel.transform.localScale = Vector3.zero;

        // Форматируем время: если больше 24 часов, пишем дни
        string timeStr;
        if (span.TotalDays >= 1)
            timeStr = string.Format("{0}д {1:D2}ч {2:D2}м", (int)span.TotalDays, span.Hours, span.Minutes);
        else
            timeStr = string.Format("{0:D2}ч {1:D2}м {2:D2}с", span.Hours, span.Minutes, span.Seconds);

        earnedText.text = $"Вас не было\n <color=#B000FF>{timeStr}</color>.\nКультисты собрали\n <color=green>{BigNumberFormatter.Format(amount)}</color> скверны!";

        // Анимация как в мини-игре
        welcomePanel.transform.DOKill();
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