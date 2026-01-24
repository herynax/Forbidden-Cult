using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class MemoryCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int cardID;
    [SerializeField] private GameObject faceSide;
    [SerializeField] private GameObject backSide;
    [SerializeField] private Image iconImage;

    private MemoryGameController controller;
    public bool isFlipped = false;
    private bool isSelected = false;
    private RectTransform rect;
    private Vector2 startPos;
    private Tween idleTween;

    public void Init(int id, Sprite icon, Color iconColor, MemoryGameController ctrl)
    {
        cardID = id;
        controller = ctrl;
        rect = GetComponent<RectTransform>();

        // УСТАНОВКА ИКОНКИ И ЦВЕТА
        iconImage.sprite = icon;
        iconImage.color = iconColor; // КРАСИМ ИКОНКУ

        // Сначала показываем лицо
        isFlipped = true;
        faceSide.SetActive(true);
        backSide.SetActive(false);

        // Анимация появления...
        rect.localScale = Vector3.zero;
        rect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(Random.Range(0f, 0.3f)).SetLink(gameObject);
    }

    // Тот самый пропавший метод для компонента Button
    public void OnClick()
    {
        // Если карта уже открыта или сейчас анимация другого процесса - игнорим
        if (isFlipped || !controller.canClick) return;

        controller.OnCardSelected(this);
    }

    // Метод фиксации "дома" и запуска вечного шевеления
    public void FixPositionAndStartIdle()
    {
        startPos = rect.anchoredPosition;
        StartIdle();
    }

    private void StartIdle()
    {
        if (idleTween != null) idleTween.Kill();

        // Небольшое покачивание вверх-вниз относительно зафиксированной точки
        idleTween = rect.DOAnchorPosY(startPos.y + 5f, 1.5f + Random.Range(0f, 1f))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    // ХОВЕР ЭФФЕКТЫ
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!controller.canClick || isFlipped) return;
        rect.DOScale(1.1f, 0.2f).SetEase(Ease.OutCubic).SetLink(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;
        rect.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic).SetLink(gameObject);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        rect.DOScale(selected ? 1.1f : 1.0f, 0.2f).SetLink(gameObject);
    }

    public void Flip(bool showFace, float duration = 0.3f)
    {
        isFlipped = showFace;

        // При повороте крутим только Rotation, не трогая Position
        rect.DORotate(new Vector3(0, 90, 0), duration / 2).SetEase(Ease.InQuad).SetLink(gameObject).OnComplete(() =>
        {
            if (this == null) return;
            faceSide.SetActive(showFace);
            backSide.SetActive(!showFace);

            rect.DORotate(Vector3.zero, duration / 2).SetEase(Ease.OutQuad);
        });
    }

    public void ShakeError()
    {
        // Тряска
        rect.DOShakePosition(0.4f, 10f, 20).SetLink(gameObject);
        // Красный мырг
        var img = backSide.GetComponent<Image>();
        img.DOColor(Color.red, 0.1f).OnComplete(() => img.DOColor(Color.white, 0.3f));
    }

    public void FlyToCenter(Vector3 centerPos)
    {
        transform.SetAsLastSibling();
        rect.DOKill(); // Убиваем всё (включая айдл) перед полетом

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOMove(centerPos, 0.6f).SetEase(Ease.InBack));
        seq.Join(rect.DORotate(new Vector3(0, 0, 360), 0.6f, RotateMode.FastBeyond360));
        seq.Join(rect.DOScale(Vector3.zero, 0.6f));
        seq.SetLink(gameObject).OnComplete(() => { if (this != null) gameObject.SetActive(false); });
    }
}