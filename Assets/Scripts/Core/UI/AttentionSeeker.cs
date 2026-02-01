using UnityEngine;
using DG.Tweening;

public class AttentionSeeker : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float pulseScale = 1.1f;    // Размер при увеличении
    [SerializeField] private float pulseDuration = 0.8f; // Скорость пульсации

    [Header("Shake Settings")]
    [SerializeField] private float shakeStrength = 15f;  // Сила тряски
    [SerializeField] private float shakeInterval = 3f;  // Как часто трясти (раз в 3 секунды)

    private Vector3 initialScale;
    private Sequence mainSequence;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Сбрасываем состояние
        transform.localScale = initialScale;
        StartJuice();
    }

    private void StartJuice()
    {
        // Убиваем старое, если есть
        mainSequence?.Kill();
        transform.DOKill();

        // 1. Создаем вечную пульсацию (эффект "дыхания")
        transform.DOScale(initialScale * pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true) // Чтобы работало даже если игра на паузе
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        // Обязательно чистим всё при выключении панели
        transform.DOKill();
        CancelInvoke();
    }
}