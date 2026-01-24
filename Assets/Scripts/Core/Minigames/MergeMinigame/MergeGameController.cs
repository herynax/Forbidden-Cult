using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

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

    [Header("Logic")]
    public float baseTime = 60f;
    public float timeBonus = 3f;
    private float timeLeft;
    private double sessionEarnings;
    private MergeItem currentItem;
    private bool canDrop = true;
    private bool isGameOver = false;

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

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clampedX = Mathf.Clamp(mousePos.x, -2.5f, 2.5f); // Настрой под свою коробку!
        currentItem.transform.position = new Vector3(clampedX, spawnPoint.position.y, 0);

        DrawGuideLine(currentItem.transform.position);

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

        // Убиваем все твины этих объектов ПЕРЕД удалением
        a.transform.DOKill();
        b.transform.DOKill();

        // Эффект слияния
        a.transform.DOMove(spawnPos, 0.1f);
        b.transform.DOMove(spawnPos, 0.1f).OnComplete(() => {

            // Удаляем старые
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            // Спавним новый уровень
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

            // Награда
            AddReward(rewardAmount, spawnPos);
        });
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
            sessionMoneyHUD.text = $"Заработано: {BigNumberFormatter.Format(sessionEarnings)}";
            sessionMoneyHUD.transform.DOKill(true);
            sessionMoneyHUD.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }

    private void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (saveManager != null) saveManager.Save();

        earnedTextOnLosePanel.text = $"Вы заработали: {BigNumberFormatter.Format(sessionEarnings)}";
        losePanel.SetActive(true);
        losePanel.transform.DOKill();
        losePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }
}