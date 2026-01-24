using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using DG.Tweening; // Не забываем DOTween

public class MiniGameButton : MonoBehaviour
{
    public MiniGameSO gameData;

    [Header("UI References")]
    [SerializeField] private GameObject readyVisual;  // Объект, который загорается, когда можно кликать
    [SerializeField] private Button mainButton;

    private SaveManager saveManager;
    private bool isUnlocked;  // Здания куплены?
    private bool isReady;     // Таймер прошел, можно играть?
    private bool hasScaledUp; // Кнопка уже появилась на экране?

    private void Start()
    {
        saveManager = FindFirstObjectByType<SaveManager>();

        // В начале кнопка полностью скрыта
        transform.localScale = Vector3.zero;

        readyVisual.SetActive(false);
        mainButton.interactable = false;

        StartCoroutine(ButtonLogicRoutine());
    }

    private IEnumerator ButtonLogicRoutine()
    {
        while (true)
        {
            // 1. Проверяем, выполнены ли условия открытия
            bool conditionsMet = gameData.RequiredUpgradeIDs.All(id =>
                saveManager.data.Upgrades.Any(u => u.ID == id && u.Amount > 0));

            // 2. Если условия выполнены впервые — анимируем появление кнопки
            if (conditionsMet && !hasScaledUp)
            {
                RevealButton();
            }

            // 3. Если кнопка уже на экране, но игра еще не готова к запуску
            if (hasScaledUp && !isReady)
            {
                // Ждем случайное время перезарядки
                float waitTime = Random.Range(gameData.minCooldown, gameData.maxCooldown);
                yield return new WaitForSeconds(waitTime);

                // Активируем кнопку
                SetReady();
            }

            yield return new WaitForSeconds(2f); // Проверка условий раз в пару секунд
        }
    }

    private void RevealButton()
    {
        hasScaledUp = true;
        isUnlocked = true;

        // Плавное появление из нуля в единицу с эффектом пружины
        transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack);
    }

    private void SetReady()
    {
        isReady = true;
        mainButton.interactable = true;

        // Включаем визуал "Готово" (например, свечение)
        readyVisual.SetActive(true);
        readyVisual.transform.localScale = Vector3.zero;
        readyVisual.transform.DOScale(1f, 0.3f).SetEase(Ease.OutCubic);

        // Можно добавить легкую пульсацию всей кнопки, пока она ждет клика
        transform.DOShakePosition(0.5f, 5f, 10, 90, false, false).SetDelay(1f).SetLoops(-1, LoopType.Restart);
    }

    public void OnClick()
    {
        if (isReady)
        {
            // Останавливаем все анимации на кнопке перед уходом
            transform.DOKill();

            saveManager.Save();
            SceneLoader.Instance.LoadScene(gameData.SceneName);
        }
    }
}