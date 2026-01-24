using UnityEngine;
using DG.Tweening;

public class MergeItem : MonoBehaviour
{
    public MergeObjectSO data;
    public bool isDropped = false;
    public bool isMerging = false;
    private MergeGameController controller;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(MergeObjectSO so, MergeGameController ctrl)
    {
        data = so;
        controller = ctrl;

        // Устанавливаем спрайт и цвет из данных SO
        sr.sprite = data.sprite;
        sr.color = data.itemColor; // КРАСИМ ОБЪЕКТ

        transform.localScale = Vector3.one * data.scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropped || isMerging) return;

        MergeItem other = collision.gameObject.GetComponent<MergeItem>();

        if (other != null && other.isDropped && !other.isMerging && other.data.level == this.data.level)
        {
            if (this.gameObject.GetInstanceID() < other.gameObject.GetInstanceID())
            {
                isMerging = true;
                other.isMerging = true;
                controller.MergeItems(this, other);
            }
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}