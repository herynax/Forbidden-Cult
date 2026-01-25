using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Lean.Pool;
using DG.Tweening.Core.Easing;
using FMODUnity;

public class CthulhuClicker : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public GameObject numberPrefab; // Префаб с FloatingNumber
    public Transform uiCanvas;      // Ссылка на Canvas

    [Header("Animation")]
    [SerializeField] private float punchStrength = 0.1f;
    [SerializeField] private float animDuration = 0.15f;

    private SaveManager saveManager;
    private RectTransform rect;
    private PassiveIncomeManager passiveIncomeManager;
    private double clickPower;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        saveManager = FindFirstObjectByType<SaveManager>();

        passiveIncomeManager = FindFirstObjectByType<PassiveIncomeManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        clickPower = 1 + (passiveIncomeManager.TotalIncomePerSecond * 0.01);
        saveManager.data.Money += clickPower;

        RuntimeManager.PlayOneShot("event:/UI/Click");

        MiniGameButton.ReduceAllCooldowns(1.0f);

        // 2. Визуальный эффект Ктулху (дерганье)
        AnimateCthulhu();

        // 3. Вылет цифры
        SpawnNumber(eventData.position);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Обязательно убиваем текущий твин, чтобы анимации не суммировались
        rect.DOKill();

        // Target: 1.1f
        // Duration: 0.6f (для эластичности нужно чуть больше времени, чтобы успели пройти колебания)
        // Ease: OutElastic — создаёт эффект пружины
        rect.DOScale(1.1f, 0.6f).SetEase(Ease.OutElastic).SetLink(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        // Возвращаемся к 1.0. Используем OutBack, чтобы был легкий "вжим" перед остановкой
        rect.DOScale(1.0f, 0.4f).SetEase(Ease.OutBack).SetLink(gameObject);
    }

    private void AnimateCthulhu()
    {
        // Убиваем текущий твин, чтобы анимации не "стакались" при быстром клике
        // true в конце означает "дойти до конца перед смертью", чтобы объект не застрял в сжатом виде
        transform.DOKill(true);

        // Делаем легкий "толчок" (punch) масштаба
        transform.DOPunchScale(new Vector3(punchStrength, -punchStrength, 0), animDuration, 1, 0.5f);
    }

    private void SpawnNumber(Vector2 pos)
    {
        // Берем объект из Lean Pool
        GameObject obj = LeanPool.Spawn(numberPrefab, uiCanvas);
        obj.GetComponent<FloatingNumber>().Initialize(clickPower, pos);
    }
}