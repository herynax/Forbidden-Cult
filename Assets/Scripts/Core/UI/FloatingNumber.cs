using UnityEngine;
using TMPro;
using DG.Tweening;
using Lean.Pool;

public class FloatingNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float moveDistance = 100f;

    // Метод вызывается Lean Pool при "спавне" из пула
    public void Initialize(double amount, Vector2 position)
    {
        transform.position = position;
        text.text = $"+{amount}";
        text.alpha = 1f;

        // Убиваем старые твины на этом объекте, если они были
        transform.DOKill();
        text.DOKill();

        // Анимация взлета
        transform.DOMoveY(position.y + moveDistance, duration)
            .SetEase(Ease.OutCubic);

        // Анимация исчезновения
        text.DOFade(0, duration)
            .SetEase(Ease.InExpo)
            .OnComplete(() => LeanPool.Despawn(gameObject)); // Возвращаем в пул
    }

    private void OnDisable()
    {
        // На всякий случай убиваем твины при выключении
        transform.DOKill();
        text.DOKill();
    }
}