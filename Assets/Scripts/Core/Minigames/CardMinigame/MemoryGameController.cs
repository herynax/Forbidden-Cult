using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class MemoryGameController : MonoBehaviour
{
    [Header("Settings")]
    public GameObject cardPrefab;
    public Transform gridContainer;
    public List<Sprite> allPossibleIcons;

    [Header("UI")]
    public Image timerBar; // Image с типом Filled
    public Color timerNormalColor = Color.green;
    public Color timerWarningColor = Color.red;

    [Header("GameOver UI")]
    public GameObject losePanel; // Ссылка на объект панели
    public Button exitButton;

    [Header("Timers & Difficulty")]
    public float previewDuration = 3f;
    public float baseTime = 30f;
    public float timePenalty = 3f;

    [Header("Juice UI")]
    [SerializeField] private TextMeshProUGUI roundOverlayText; // Текст в центре экрана (не статический)
    [SerializeField] private CanvasGroup roundOverlayGroup;

    [Header("Mini Game Stats")]
    [SerializeField] private TextMeshProUGUI earnedTextOnLosePanel; // Текст "Вы заработали: N"
    [SerializeField] private GameObject moneyNumberPrefab;       // Префаб вылетающей цифры
    [SerializeField] private Transform uiCanvasTransform;        // Канвас для цифр

    [Header("Live HUD")]
    [SerializeField] private TextMeshProUGUI sessionMoneyHUD; // Текст, который виден всегда
    [SerializeField] private float hudPunchAmount = 1.2f;   // На сколько увеличится текст при получении денег

    private double sessionEarnings;

    [Header("Card Colors")]
    public List<Color> cardIDColors;

    private int currentRound = 1;
    private float timeLeft;
    private float maxTime;
    private float visualTime; // Для плавного лерпа слайдера

    private List<MemoryCard> spawnedCards = new List<MemoryCard>();
    private MemoryCard firstSelected, secondSelected;

    private bool isTimerActive = false;

    [HideInInspector] public bool canClick = false;
    private int pairsToMatch;
    private SaveManager saveManager;

    private void Start()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
        sessionEarnings = 0;

        // Сразу обновляем текст в начале
        UpdateEarningsHUD();

        losePanel.SetActive(false);
        losePanel.transform.localScale = Vector3.zero;
        exitButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("MainClickerScene"));

        StartRound();
    }

    private void StartRound()
    {
        canClick = false;
        isTimerActive = false;

        // Показываем надпись "ROUND N"
        ShowRoundOverlay();

        maxTime = baseTime - (currentRound * 1.5f);
        if (maxTime < 10f) maxTime = 10f;
        timeLeft = maxTime;
        visualTime = maxTime;

        pairsToMatch = 2 + (currentRound / 2);
        GenerateGrid();
    }

    private void ShowRoundOverlay()
    {
        roundOverlayText.text = "РАУНД " + currentRound;
        roundOverlayGroup.alpha = 0;
        roundOverlayText.transform.localScale = Vector3.one * 0.5f;

        Sequence seq = DOTween.Sequence();
        seq.Append(roundOverlayGroup.DOFade(1, 0.5f));
        seq.Join(roundOverlayText.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
        seq.AppendInterval(1f); // Висит секунду
        seq.Append(roundOverlayGroup.DOFade(0, 0.5f));
        seq.Join(roundOverlayText.transform.DOScale(1.5f, 0.5f).SetEase(Ease.InQuad));
    }

    private void GenerateGrid()
    {
        // Включаем сетку
        var gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        gridLayout.enabled = true;

        // --- ИСПРАВЛЕННЫЙ БЛОК ОЧИСТКИ ---
        foreach (var c in spawnedCards)
        {
            if (c != null)
            {
                // 1. Убиваем все твины на этом объекте ПЕРЕД удалением
                c.transform.DOKill();
                // 2. Удаляем
                Destroy(c.gameObject);
            }
        }
        spawnedCards.Clear();
        // ---------------------------------

        List<int> ids = new List<int>();
        for (int i = 0; i < pairsToMatch; i++) { ids.Add(i); ids.Add(i); }
        ids.Shuffle();

        foreach (int id in ids)
        {
            GameObject go = Instantiate(cardPrefab, gridContainer);
            MemoryCard card = go.GetComponent<MemoryCard>();

            // ВЫБИРАЕМ ЦВЕТ:
            // Если ID выходит за пределы списка цветов, используем белый (или зацикливаем через %)
            Color targetColor = Color.white;
            if (id < cardIDColors.Count)
            {
                targetColor = cardIDColors[id];
            }
            else if (cardIDColors.Count > 0)
            {
                targetColor = cardIDColors[id % cardIDColors.Count]; // Зацикливание, если цветов меньше чем ID
            }

            // Передаем цвет в Init
            card.Init(id, allPossibleIcons[id % allPossibleIcons.Count], targetColor, this);
            spawnedCards.Add(card);
        }

        Canvas.ForceUpdateCanvases();
        StartCoroutine(DisableGridAfterLayout());
        StartCoroutine(PreviewSequence());
    }

    private IEnumerator DisableGridAfterLayout()
    {
        // Ждем, пока UI прогрузится
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Выключаем сетку
        gridContainer.GetComponent<GridLayoutGroup>().enabled = false;

        // Теперь заставляем каждую карту запомнить свою текущую позицию и начать айдл
        foreach (var card in spawnedCards)
        {
            if (card != null) card.FixPositionAndStartIdle();
        }
    }

    private IEnumerator PreviewSequence()
    {
        timerBar.fillAmount = 1;
        // 1. Ждем показа карт
        yield return new WaitForSeconds(previewDuration);

        // 2. Переворачиваем карты
        foreach (var card in spawnedCards) card.Flip(false);

        // 3. Ждем завершения анимации переворота
        yield return new WaitForSeconds(0.5f);

        // 4. ВОТ ТЕПЕРЬ запускаем игру и таймер
        canClick = true;
        isTimerActive = true;
    }

    public void OnCardSelected(MemoryCard card)
    {
        if (firstSelected == null)
        {
            firstSelected = card;
            firstSelected.SetSelected(true); // Увеличиваем
            firstSelected.Flip(true);
        }
        else if (secondSelected == null && card != firstSelected)
        {
            secondSelected = card;
            secondSelected.SetSelected(true); // Увеличиваем
            secondSelected.Flip(true);
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        canClick = false;
        yield return new WaitForSeconds(0.5f);

        if (firstSelected.cardID == secondSelected.cardID)
        {
            Vector3 centerPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            firstSelected.FlyToCenter(centerPos);
            secondSelected.FlyToCenter(centerPos);

            yield return new WaitForSeconds(0.6f);

            double reward = FindFirstObjectByType<PassiveIncomeManager>().TotalIncomePerSecond * 15;
            if (reward < 1) reward = 1;

            sessionEarnings += reward;
            saveManager.data.Money += reward;

            // ОБНОВЛЯЕМ ТЕКСТ ПЕРЕД ГЛАЗАМИ
            UpdateEarningsHUD();

            SpawnFloatingNumber(reward, centerPos);
            uiCanvasTransform.DOPunchPosition(new Vector3(10, 10, 0), 0.2f);

            pairsToMatch--;
            if (pairsToMatch <= 0)
            {
                isTimerActive = false;
                yield return new WaitForSeconds(1f);
                currentRound++;
                StartRound();
            }
        }
        else
        {
            // Ошибка... (код без изменений)
            firstSelected.ShakeError();
            secondSelected.ShakeError();
            timeLeft -= timePenalty;
            timerBar.transform.DOShakePosition(0.3f, 10f);
            yield return new WaitForSeconds(0.5f);
            firstSelected.SetSelected(false);
            secondSelected.SetSelected(false);
            firstSelected.Flip(false);
            secondSelected.Flip(false);
        }

        firstSelected = null;
        secondSelected = null;
        canClick = true;
    }

    private void UpdateEarningsHUD()
    {
        if (sessionMoneyHUD != null)
        {
            sessionMoneyHUD.text = $"Заработано: {BigNumberFormatter.Format(sessionEarnings)}";

            // "Сочный" эффект: текст слегка подпрыгивает при изменении
            sessionMoneyHUD.transform.DOKill(true);
            sessionMoneyHUD.transform.DOPunchScale(Vector3.one * (hudPunchAmount - 1f), 0.2f);
        }
    }

    private void Update()
    {
        // Если таймер еще не запущен, держим полоску полной и выходим
        if (!isTimerActive)
        {
            timerBar.fillAmount = 1f;
            return;
        }

        // Основная логика убывания времени
        timeLeft -= Time.deltaTime;

        // Плавный лерп для красоты
        visualTime = Mathf.Lerp(visualTime, timeLeft, Time.deltaTime * 5f);
        timerBar.fillAmount = Mathf.Clamp01(visualTime / maxTime);

        if (timeLeft <= 0)
        {
            isTimerActive = false; // Останавливаем, чтобы не заходить сюда дважды
            GameOver();
        }
    }

    private void SpawnFloatingNumber(double amount, Vector3 position)
    {
        // Используем тот же метод, что и в кликере
        GameObject go = Lean.Pool.LeanPool.Spawn(moneyNumberPrefab, position, Quaternion.identity, uiCanvasTransform);
        var floatingNum = go.GetComponent<FloatingNumber>(); // Убедись, что скрипт FloatingNumber из начала чата на месте
        if (floatingNum != null)
        {
            floatingNum.Initialize(amount, position);
        }
    }


    private void GameOver()
    {
        canClick = false;
        isTimerActive = false;
        saveManager.Save();

        // Заполняем текст на панельке
        earnedTextOnLosePanel.text = $"Вы заработали: {BigNumberFormatter.Format(sessionEarnings)}";

        losePanel.SetActive(true);
        losePanel.transform.localScale = Vector3.zero;
        losePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }
}

// Вспомогательный класс для перемешивания списка
public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}