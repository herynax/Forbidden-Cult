using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using FMODUnity;

public class BaseButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float downScale = 0.95f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    protected Vector3 originalScale;
    private CanvasGroup canvasGroup;

    public virtual void Awake()
    {
        originalScale = transform.localScale;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Принудительная зачистка при выключении или смене сцены
    public virtual void OnDisable()
    {
        transform.DOKill();
    }

    public virtual void OnDestroy()
    {
        // Самая важная зачистка для WebGL и смены сцен
        transform.DOKill();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();

        transform.DOScale(originalScale * hoverScale, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // 1. Убиваем текущий твин ПЕРЕД запуском нового
        transform.DOKill();

        transform.DOScale(originalScale, scaleDuration)
            .SetEase(Ease.OutSine)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();

        transform.DOScale(originalScale * downScale, 0.1f)
            .SetUpdate(true)
            .SetLink(gameObject);

        RuntimeManager.PlayOneShot("event:/UI/Click");
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();

        // Проверка: жива ли еще кнопка (защита от ошибок при смене сцены)
        if (gameObject == null) return;

        float targetS = eventData.hovered != null && eventData.hovered.Contains(gameObject) ? hoverScale : 1f;

        transform.DOScale(originalScale * targetS, 0.1f)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}