using UnityEngine;
using TMPro;
using DG.Tweening;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float duration = 0.1f;

    private SaveManager saveManager;
    private double lastMoney;
    private bool isReady = false;

    private void Start()
    {
        // При старте проверяем наличие ссылки на текст
        if (moneyText == null)
        {
            Debug.LogError($"[CurrencyDisplay] На объекте {gameObject.name} не назначен moneyText!");
            return;
        }

        // Пытаемся найти менеджер в первый раз
        TryInit();
    }

    private void Update()
    {
        // Если еще не готовы, пробуем инициализироваться
        if (!isReady)
        {
            TryInit();
            return; // Выходим из Update, пока не найдем данные
        }

        // Если по какой-то причине менеджер или данные пропали (смена сцены)
        if (saveManager == null || saveManager.data == null)
        {
            isReady = false;
            return;
        }

        // Если всё ок — обновляем логику
        if (saveManager.data.Money != lastMoney)
        {
            UpdateDisplay(false);
            lastMoney = saveManager.data.Money;
        }
    }

    private void TryInit()
    {
        if (saveManager == null)
        {
            saveManager = Object.FindFirstObjectByType<SaveManager>();
        }

        // Проверяем не только наличие менеджера, но и наличие данных внутри него
        if (saveManager != null && saveManager.data != null)
        {
            lastMoney = saveManager.data.Money;
            UpdateDisplay(true);
            isReady = true;
        }
    }

    private void UpdateDisplay(bool instant)
    {
        if (moneyText == null) return;

        // Показываем текст
        moneyText.text = BigNumberFormatter.Format(saveManager.data.Money);

        if (!instant)
        {
            // Анимация подпрыгивания
            moneyText.transform.DOKill(true);
            moneyText.transform.DOPunchScale(Vector3.one * (punchScale - 1f), duration);
        }
    }
}