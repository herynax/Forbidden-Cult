using UnityEngine;

public class DangerZone : MonoBehaviour
{
    private MergeGameController controller;
    private float penaltyCooldown = 1f; // Чтобы штраф не списывался каждый кадр
    private float nextPenaltyTime;

    private void Start()
    {
        controller = Object.FindFirstObjectByType<MergeGameController>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Проверяем, что это упавший объект
        MergeItem item = collision.GetComponent<MergeItem>();

        if (item != null && item.isDropped && !item.isMerging)
        {
            if (Time.time >= nextPenaltyTime)
            {
                collision.gameObject.SetActive(false);
                controller.ApplyDangerPenalty();
                nextPenaltyTime = Time.time + penaltyCooldown;
            }
        }
    }
}