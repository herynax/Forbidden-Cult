using UnityEngine;

public class ContinuousFollowCursorRotation : MonoBehaviour
{
    public float rotationRange = 30f; // Максимальный угол в градусах
    public float rotationSpeed = 10f; // Скорость плавного вращения

    private Camera cam;

    void Start()
    {
        cam = Camera.main; // Камера по умолчанию
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldMousePos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));

        // Направление к курсору
        Vector3 direction = worldMousePos - transform.position;
        float angleToMouse = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Ограничение по диапазону
        float targetZ = Mathf.Clamp(angleToMouse, -rotationRange, rotationRange);

        // Текущий угол
        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360; // переводим в диапазон (-180, 180)

        // Плавное вращение
        float newZ = Mathf.Lerp(currentZ, targetZ, Time.deltaTime * rotationSpeed);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }
}