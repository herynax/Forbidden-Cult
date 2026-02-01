using UnityEngine;

public class FollowMouseWithLimit : MonoBehaviour
{
    // Параметры настройки
    public float followSpeed = 10f;        // скорость следования
    public float stretchFactor = 0.5f;     // масштабное растяжение
    public float maxDistance = 5f;         // максимальное расстояние от начальной точки

    private Vector3 initialPosition;       // стартовая позиция объекта
    private Vector3 targetPosition;        // текущая целевая позиция за курсором
    private Camera cam;

    void Start()
    {
        initialPosition = transform.position;
        cam = Camera.main;
    }

    void Update()
    {
        // Получение позиции мыши в мировых координатах
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldMousePos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));

        // Задаем целевую позицию
        targetPosition = new Vector3(worldMousePos.x, worldMousePos.y, transform.position.z);

        // Ограничение по расстоянию от начальной точки
        Vector3 offset = targetPosition - initialPosition;
        if (offset.magnitude > maxDistance)
        {
            offset = offset.normalized * maxDistance;
            targetPosition = initialPosition + offset;
        }

        // Плавное следование к цели
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Эффект "тянучки" с масштабированием
        float distance = Vector3.Distance(transform.position, targetPosition);
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * (1 + stretchFactor * distance / maxDistance),
            followSpeed * Time.deltaTime
        );
    }
}