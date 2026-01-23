using UnityEngine;
using TMPro;
using DG.Tweening;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private SaveManager saveManager;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float duration = 0.1f;

    private double lastMoney;

    private void Start()
    {
        UpdateDisplay(true);
        lastMoney = saveManager.data.Money;
    }

    private void Update()
    {
        // ≈сли значение изменилось (был клик или сработал пассивный доход)
        if (saveManager.data.Money != lastMoney)
        {
            UpdateDisplay(false);
            lastMoney = saveManager.data.Money;
        }
    }

    private void UpdateDisplay(bool instant)
    {
        moneyText.text = BigNumberFormatter.Format(saveManager.data.Money);

        if (!instant)
        {
            // Ёффект "подпрыгивани€" текста при изменении счета
            moneyText.transform.DOKill(true);
            moneyText.transform.DOPunchScale(Vector3.one * (punchScale - 1f), duration);
        }
    }
}