using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Lean.Pool;
using FMODUnity;

public class CthulhuClicker : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public GameObject numberPrefab; // Префаб с FloatingNumber (UI-текст)
    public Transform uiCanvas;      // Ссылка на Canvas, где спавнятся цифры

    [Header("Animation")]
    [SerializeField] private float punchStrength = 0.1f;
    [SerializeField] private float animDuration = 0.15f;
    [SerializeField] private float hoverScale = 1.1f;

    private SaveManager saveManager;
    private PassiveIncomeManager passiveIncomeManager;
    private double clickPower;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveIncomeManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 1. Логика расчета
        clickPower = 1 + (passiveIncomeManager.TotalIncomePerSecond * 0.01);
        saveManager.data.Money += clickPower;

        // 2. Звук и КД мини-игр
        RuntimeManager.PlayOneShot("event:/UI/Click");
        MiniGameButton.ReduceAllCooldowns(1.0f);

        // 3. Визуальный эффект (дерганье)
        AnimateCthulhu();

        // 4. Вылет цифры
        // eventData.position возвращает экранные координаты, что идеально для UI цифр
        SpawnNumber(eventData.position);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        // Используем initialScale, чтобы корректно увеличивать объект
        transform.DOScale(initialScale * hoverScale, 0.6f)
            .SetEase(Ease.OutElastic)
            .SetLink(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(initialScale, 0.4f)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);
    }

    private void AnimateCthulhu()
    {
        // Сбрасываем к hoverScale перед панчем, чтобы не было скачков
        transform.DOKill(true);

        // Эффект "сжатия-разжатия"
        transform.DOPunchScale(new Vector3(punchStrength, -punchStrength, 0), animDuration, 1, 0.5f)
            .SetLink(gameObject);
    }

    private void SpawnNumber(Vector2 screenPos)
    {
        if (numberPrefab == null || uiCanvas == null) return;

        // Берем объект из Lean Pool и спавним его в Canvas
        GameObject obj = LeanPool.Spawn(numberPrefab, uiCanvas);

        // Передаем сырое значение clickPower (внутри FloatingNumber оно отформатируется)
        obj.GetComponent<FloatingNumber>().Initialize(clickPower, screenPos);
    }
}