using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Lean.Pool;
using DG.Tweening.Core.Easing;

public class CthulhuClicker : MonoBehaviour, IPointerDownHandler
{
    [Header("Settings")]
    public GameObject numberPrefab; // Префаб с FloatingNumber
    public Transform uiCanvas;      // Ссылка на Canvas

    [Header("Animation")]
    [SerializeField] private float punchStrength = 0.1f;
    [SerializeField] private float animDuration = 0.15f;

    private SaveManager saveManager; // Ссылка на твой менеджер сохранений

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 1. Логика ресурса
        saveManager.data.Money += saveManager.data.ClickPower;

        // 2. Визуальный эффект Ктулху (дерганье)
        AnimateCthulhu();

        // 3. Вылет цифры
        SpawnNumber(eventData.position);
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
        obj.GetComponent<FloatingNumber>().Initialize(saveManager.data.ClickPower, pos);
    }
}