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
        // Используем более надежный поиск, так как менеджеры могут быть на разных объектах
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();

        // Проверка: если мы забыли что-то перетащить в инспекторе
        if (welcomePanel == null || earnedText == null || closeButton == null)
        {
            Debug.LogError("[OfflineIncomeManager] Не все ссылки на UI назначены в инспекторе!");
            return;
        }

        // Ждем небольшую задержку, чтобы PassiveIncomeManager успел инициализироваться и посчитать CPS
        DOVirtual.DelayedCall(0.1f, () => {
            CalculateOfflineIncome();
        }).SetUpdate(true);

        closeButton.onClick.AddListener(ClosePanel);
    }

    private void CalculateOfflineIncome()
    {
        // Проверяем наличие всех данных перед расчетом
        if (saveManager == null || saveManager.data == null || passiveManager == null) return;

        // Если это самый первый запуск игры и времени сохранения еще нет
        if (saveManager.data.LastSaveTimeTicks <= 0) return;

        // 1. Считаем сколько времени прошло
        System.DateTime lastTime = new System.DateTime(saveManager.data.LastSaveTimeTicks);
        System.DateTime currentTime = System.DateTime.UtcNow;
        System.TimeSpan timePassed = currentTime - lastTime;

        double secondsOffline = timePassed.TotalSeconds;

        // Если прошло меньше 10 секунд (например, просто перезагрузил страницу), не спамим
        if (secondsOffline < 10) return;

        // 2. Считаем доход
        double cps = passiveManager.TotalIncomePerSecond;
        double earned = cps * secondsOffline;

        if (earned > 0.01)
        {
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

        earnedText.text = $"Вас не было <color=#B000FF>{timeStr}</color>.\nКультисты собрали <color=green>{BigNumberFormatter.Format(amount)}</color> скверны!";

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