using UnityEngine;

public class AnimateObject : MonoBehaviour
{
    // Настройка диапазона вращения по осям Y и Z
    public float rotationAmplitudeY = 30f; // амплитуда по Y
    public float rotationAmplitudeZ = 20f; // амплитуда по Z
    public float rotationSpeed = 1f; // скорость цикла вращения

    // Максимальный диапазон изменения масштаба
    public Vector3 scaleRange = new Vector3(0.1f, 0.1f, 0.1f);
    private Vector3 initialScale;
    private float scaleTime;

    void Start()
    {
        initialScale = transform.localScale;
        scaleTime = 0f;
    }

    void Update()
    {
        // Время для синусоидальной функции, создающей цикл «туда и обратно»
        float t = Time.time * rotationSpeed;

        // Поворот по осям Y и Z в диапазоне, используя синусоиду
        float rotY = Mathf.Sin(t) * rotationAmplitudeY;
        float rotZ = Mathf.Sin(t) * rotationAmplitudeZ;

        // Обновляем локальный EulerAngles
        transform.localEulerAngles = new Vector3(0, rotY, rotZ);

        // Меняем масштаб в циклe
        scaleTime += Time.deltaTime;
        float scaleFactor = 1 + Mathf.Sin(scaleTime) * 0.1f; // амплитуда 0.1
        transform.localScale = initialScale * scaleFactor;
    }
}