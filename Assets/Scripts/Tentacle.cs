using UnityEngine;

public class AnimateObjectSimple : MonoBehaviour
{
    [Header("Вращение (Swing)")]
    public Vector3 rotationAmount = new Vector3(0, 30, 20); // Макс. отклонение
    public float rotationSpeed = 2f;

    [Header("Пульсация (Pulse)")]
    public float scaleAmount = 0.1f;
    public float scaleSpeed = 1.5f;

    private Vector3 initialRotation;
    private Vector3 initialScale;
    private float randomOffset;

    void Start()
    {
        // Сохраняем позы, выставленные в редакторе
        initialRotation = transform.localEulerAngles;
        initialScale = transform.localScale;

        // Генерируем случайное число, чтобы "сдвинуть" фазу синуса
        // Это и дает ту самую асинхронность
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Вычисляем время со сдвигом
        float timeWithOffset = Time.time + randomOffset;

        // --- ЛОГИКА ВРАЩЕНИЯ ---
        // Mathf.Sin выдает значение от -1 до 1
        float sinRot = Mathf.Sin(timeWithOffset * rotationSpeed);

        // Считаем новый угол относительно начального
        float newRotY = initialRotation.y + (sinRot * rotationAmount.y);
        float newRotZ = initialRotation.z + (sinRot * rotationAmount.z);

        transform.localEulerAngles = new Vector3(initialRotation.x, newRotY, newRotZ);

        // --- ЛОГИКА МАСШТАБА ---
        // Используем Cos, чтобы фазы скейла и ротации не совпадали идеально
        float cosScale = Mathf.Cos(timeWithOffset * scaleSpeed);
        float currentScaleFactor = 1f + (cosScale * scaleAmount);

        transform.localScale = initialScale * currentScaleFactor;
    }
}