using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using DG.Tweening;

public class MiniGameButton : MonoBehaviour
{
    public MiniGameSO gameData;

    [Header("UI References")]
    [SerializeField] private GameObject readyVisual;  // Эффект свечения (активен только в состоянии 3)
    [SerializeField] private Button mainButton;
    [SerializeField] private CanvasGroup canvasGroup; // Для управления прозрачностью в состоянии 2

    private SaveManager saveManager;
    private bool isUnlocked;  // Состояние 1 пройдено?
    private bool isReady;     // Состояние 3 наступило?
    private bool hasRevealed; // Флаг, чтобы не проигрывать анимацию появления каждый раз

    private void Start()
    {
        saveManager = FindFirstObjectByType<SaveManager>();

        // Изначально кнопка в Состоянии 1 (Скрыта)
        transform.localScale = Vector3.zero;
        readyVisual.SetActive(false);
        mainButton.interactable = false;
        if (canvasGroup != null) canvasGroup.alpha = 0.5f; // Полупрозрачная в ожидании

        StartCoroutine(ButtonLogicRoutine());
    }

    private IEnumerator ButtonLogicRoutine()
    {
        while (true)
        {
            // ПРОВЕРКА СОСТОЯНИЯ 1: Куплены ли здания?
            bool conditionsMet = gameData.RequiredUpgradeIDs.All(id =>
                saveManager.data.Upgrades.Any(u => u.ID == id && u.Amount > 0));

            if (conditionsMet)
            {
                // ПЕРЕХОД В СОСТОЯНИЕ 2: Появление кнопки (Ожидание)
                if (!hasRevealed)
                {
                    yield return RevealButton();
                }

                // ПРОВЕРКА: Нужно ли запускать таймер для СОСТОЯНИЯ 3?
                if (!isReady)
                {
                    float waitTime = Random.Range(gameData.minCooldown, gameData.maxCooldown);
                    Debug.Log($"Мини-игра {gameData.TypeID} будет готова через {waitTime} сек.");

                    yield return new WaitForSeconds(waitTime);

                    // ПЕРЕХОД В СОСТОЯНИЕ 3: Можно нажимать
                    SetReady();
                }
            }

            yield return new WaitForSeconds(2f); // Проверка условий раз в 2 секунды
        }
    }

    private IEnumerator RevealButton()
    {
        hasRevealed = true;
        isUnlocked = true;

        // Анимация перехода из Состояния 1 в Состояние 2
        transform.DOKill();
        yield return transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    private void SetReady()
    {
        isReady = true;
        mainButton.interactable = true;

        // Визуал Состояния 3
        if (canvasGroup != null) canvasGroup.DOFade(1f, 0.5f);

        readyVisual.SetActive(true);
        readyVisual.transform.localScale = Vector3.zero;
        readyVisual.transform.DOScale(1f, 0.3f).SetEase(Ease.OutCubic);

        // Акцентная анимация (тряска), чтобы игрок заметил кнопку
        transform.DOShakePosition(0.5f, 5f, 10).SetLoops(-1, LoopType.Restart).SetDelay(2f);
    }

    public void OnClick()
    {
        if (isReady)
        {
            // Останавливаем всё перед сменой сцены
            transform.DOKill();
            StopAllCoroutines();

            saveManager.Save();
            SceneLoader.Instance.LoadScene(gameData.SceneName);
        }
    }
}