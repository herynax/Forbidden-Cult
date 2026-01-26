using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;

public class MergeGameController : MonoBehaviour
{
    [Header("Settings")]
    public List<MergeObjectSO> allObjects;
    public GameObject itemPrefab;
    public Transform spawnPoint;
    public LineRenderer guideLine;
    public LayerMask collisionMask;

    [Header("UI HUD")]
    public Image timerBar;
    public TextMeshProUGUI sessionMoneyHUD;
    public GameObject moneyNumberPrefab;
    public Transform uiCanvasTransform; // Ссылка на Canvas или панель для цифр

    [Header("GameOver")]
    public GameObject losePanel;
    public TextMeshProUGUI earnedTextOnLosePanel;
    public Button exitButton;

    [Header("Merge Visuals")]
    public GameObject[] mergeParticlePrefabs;

    [Header("Logic")]
    public float baseTime = 60f;
    public float timeBonus = 3f;
    private float timeLeft;
    private double sessionEarnings;
    private MergeItem currentItem;
    private bool canDrop = true;
    private bool isGameOver = false;

    [Header("Difficulty Scaling")]
    [SerializeField] private float minBaseTime = 10f; // Минимальный порог, чтобы игра не стала невозможной
    [SerializeField] private float mergeTimeReduction = 0.1f; // На сколько уменьшаем базовое время за мердж

    [Header("Danger Zone")]
    [SerializeField] private float dangerPenaltyAmount = 5f;

    // КЭШ МЕНЕДЖЕРОВ
    private SaveManager saveManager;
    private PassiveIncomeManager passiveManager;

    private void Awake()
    {
        // Находим их заранее, чтобы не искать в колбэках
        saveManager = Object.FindFirstObjectByType<SaveManager>();
        passiveManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
    }

    private void Start()
    {
        timeLeft = baseTime;
        sessionEarnings = 0;
        isGameOver = false;

        losePanel.SetActive(false);
        losePanel.transform.localScale = Vector3.zero;

        // Чтобы кнопка работала через SceneLoader
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("MainClickerScene"));

        UpdateHUD();
        SpawnNewItem();
    }

    private void Update()
    {
        if (isGameOver) return;
        HandleInput();
        HandleTimer();
    }

    private void HandleInput()
    {
        if (currentItem == null || !canDrop) return;

        // 1. Получаем позицию мыши в мировых координатах
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 2. Вычисляем динамические границы экрана в мировых координатах
        // Viewport (0,0) — это левый нижний угол, (1,1) — правый верхний
        float leftBorder = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float rightBorder = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        // 3. Учитываем радиус объекта, чтобы он не заходил за край наполовину
        // Берем скейл из SO и делим на 2 (радиус)
        float objectRadius = (currentItem.data.scale) / 2f;

        // 4. Ограничиваем X с учетом границ экрана и размера объекта
        float clampedX = Mathf.Clamp(mousePos.x, leftBorder + objectRadius, rightBorder - objectRadius);

        // 5. Применяем позицию
        currentItem.transform.position = new Vector3(clampedX, spawnPoint.position.y, 0);

        // Отрисовка линии
        DrawGuideLine(currentItem.transform.position);

        // Дроп
        if (Input.GetMouseButtonDown(0)) DropItem();
    }

    private void DrawGuideLine(Vector3 pos)
    {
        guideLine.enabled = true;
        guideLine.SetPosition(0, pos);
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 20f, collisionMask);

        if (hit.collider != null)
            guideLine.SetPosition(1, hit.point);
        else
            guideLine.SetPosition(1, pos + Vector3.down * 20f);
    }

    private void DropItem()
    {
        canDrop = false;
        guideLine.enabled = false;

        currentItem.GetComponent<Rigidbody2D>().simulated = true;
        currentItem.isDropped = true;
        currentItem = null;

        DOVirtual.DelayedCall(0.8f, () => {
            if (!isGameOver) SpawnNewItem();
        }).SetLink(gameObject);
    }

    private void SpawnNewItem()
    {
        int randomIdx = Random.Range(0, Mathf.Min(3, allObjects.Count));
        GameObject go = Instantiate(itemPrefab, spawnPoint.position, Quaternion.identity);
        currentItem = go.GetComponent<MergeItem>();
        currentItem.Init(allObjects[randomIdx], this);
        currentItem.GetComponent<Rigidbody2D>().simulated = false;

        canDrop = true;
    }

    public void MergeItems(MergeItem a, MergeItem b)
    {
        if (isGameOver) return;

        Vector3 spawnPos = (a.transform.position + b.transform.position) / 2f;
        MergeObjectSO nextLevel = a.data.nextLevel;
        double rewardAmount = a.data.reward;

        // --- НОВОЕ: СПАВН ЧАСТИЦ ---
        SpawnRandomMergeParticle(spawnPos);

        RuntimeManager.PlayOneShot("event:/UI/MerdgeMinigame/Merdge");

        a.transform.DOKill();
        b.transform.DOKill();

        a.transform.DOMove(spawnPos, 0.1f);
        b.transform.DOMove(spawnPos, 0.1f).OnComplete(() => {
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            if (nextLevel != null)
            {
                GameObject newGo = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
                var newItem = newGo.GetComponent<MergeItem>();
                newItem.Init(nextLevel, this);
                newItem.isDropped = true;
                newItem.GetComponent<Rigidbody2D>().simulated = true;

                newGo.transform.localScale = Vector3.zero;
                newGo.transform.DOScale(nextLevel.scale, 0.3f).SetEase(Ease.OutBack);
            }

            ReduceBaseTime();
            AddReward(rewardAmount, spawnPos);
        });
    }

    private void ReduceBaseTime()
    {
        // Уменьшаем базовое время (максимальный объем слайдера)
        baseTime -= mergeTimeReduction;

        // Ограничиваем, чтобы не уйти в ноль
        if (baseTime < minBaseTime) baseTime = minBaseTime;
    }

    public void ApplyDangerPenalty()
    {
        if (isGameOver) return;

        // Штрафуем игрока
        timeLeft -= dangerPenaltyAmount;

        // Визуальный фидбек: трясем таймер и красим в красный
        timerBar.transform.DOKill(true);
        timerBar.transform.DOShakePosition(0.5f, 15f);
        timerBar.DOColor(Color.red, 0.2f).OnComplete(() => timerBar.DOColor(Color.green, 0.5f));

        if (timeLeft <= 0) GameOver();
    }

    private void SpawnRandomMergeParticle(Vector3 pos)
    {
        if (mergeParticlePrefabs == null || mergeParticlePrefabs.Length == 0) return;

        // Выбираем случайный индекс из массива
        int randomIndex = Random.Range(0, mergeParticlePrefabs.Length);
        GameObject selectedPrefab = mergeParticlePrefabs[randomIndex];

        if (selectedPrefab != null)
        {
            // Спавним через Lean Pool (мировые координаты)
            GameObject effect = Lean.Pool.LeanPool.Spawn(selectedPrefab, pos, Quaternion.identity);

            // Возвращаем в пул через 2 секунды (или настрой время в самом префабе через LeanDespawn)
            Lean.Pool.LeanPool.Despawn(effect, 2f);
        }
    }

    private void AddReward(double amount, Vector3 pos)
    {
        if (isGameOver) return;

        // Безопасно получаем доход
        double currentCps = (passiveManager != null) ? passiveManager.TotalIncomePerSecond : 1.0;
        double finalReward = amount * (currentCps + 1);

        sessionEarnings += finalReward;

        if (saveManager != null)
            saveManager.data.Money += finalReward;

        timeLeft += timeBonus;
        if (timeLeft > baseTime) timeLeft = baseTime; // Ограничение таймера

        UpdateHUD();
        SpawnFloatingNumber(finalReward, pos);
    }

    private void SpawnFloatingNumber(double amount, Vector3 worldPos)
    {
        if (moneyNumberPrefab == null) return;

        // Переводим из мира в экран для UI
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        GameObject go = Lean.Pool.LeanPool.Spawn(moneyNumberPrefab, screenPos, Quaternion.identity, uiCanvasTransform);
        var fn = go.GetComponent<FloatingNumber>();
        if (fn != null) fn.Initialize(amount, screenPos);
    }

    private void HandleTimer()
    {
        timeLeft -= Time.deltaTime;
        timerBar.fillAmount = Mathf.Clamp01(timeLeft / baseTime);

        if (timeLeft <= 0) GameOver();
    }

    private void UpdateHUD()
    {
        if (sessionMoneyHUD != null)
        {
            sessionMoneyHUD.text = $"Скверна: {BigNumberFormatter.Format(sessionEarnings)}";
            sessionMoneyHUD.transform.DOKill(true);
            sessionMoneyHUD.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }

    private void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (saveManager != null) saveManager.Save();

        earnedTextOnLosePanel.text = $"Скверны получено: {BigNumberFormatter.Format(sessionEarnings)}";
        losePanel.SetActive(true);
        losePanel.transform.DOKill();
        losePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }
}