using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BuildingEntity : MonoBehaviour, IPointerClickHandler
{
    public enum State { Active, Sleeping }
    public State currentState = State.Active;

    [Header("Visual Settings")]
    [SerializeField] private Image iconImage;
    private Material defaultMaterial;
    private Color myRandomColor;

    private Color originalColor;
    private Tween currentAnim;
    private Tween flashTween; // Твин для мигания шейдера
    private BuildingVisualManager manager;
    private UpgradeSO myUpgrade;

    // Логика кликов
    private int clicksNeeded;
    private int currentClicks;

    public void Init(UpgradeSO upgrade, BuildingVisualManager mngr, Color savedColor = default)
    {
        myUpgrade = upgrade;
        manager = mngr;
        iconImage = GetComponent<Image>();
        defaultMaterial = iconImage.material; // Запоминаем стандартный материал (обычно UI/Default)

        // ЛОГИКА ЦВЕТА
        if (savedColor == default || savedColor.a == 0)
        {
            if (upgrade.possibleColors != null && upgrade.possibleColors.Count > 0)
                myRandomColor = upgrade.possibleColors[Random.Range(0, upgrade.possibleColors.Count)];
            else
                myRandomColor = Color.white;
        }
        else
        {
            myRandomColor = savedColor;
        }

        iconImage.color = myRandomColor;

        // Регистрация в менеджере доходов
        var piManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
        if (piManager != null) piManager.RegisterBuilding(this);

        StartActiveAnimation();
        InvokeRepeating(nameof(TryToSleep), Random.Range(10f, 20f), 15f);
    }

    public double GetIncomeValue() => (myUpgrade != null) ? myUpgrade.BasePassiveIncome : 0;
    public string GetUpgradeID() => (myUpgrade != null) ? myUpgrade.ID : "";
    public Color GetCurrentColor() => myRandomColor;

    private void TryToSleep()
    {
        if (currentState == State.Sleeping) return;
        if (Random.value < 0.1f) GoToSleep();
    }

    private void GoToSleep()
    {
        currentState = State.Sleeping;
        clicksNeeded = Random.Range(myUpgrade.MinClicksToWake, myUpgrade.MaxClicksToWake + 1);
        currentClicks = 0;

        // 1. МЕНЯЕМ МАТЕРИАЛ
        if (myUpgrade.sleepMaterial != null)
        {
            iconImage.material = myUpgrade.sleepMaterial;

            // 2. ЗАПУСКАЕМ МИГАНИЕ ШЕЙДЕРА (_FlashAmount 0 -> 1)
            // Убиваем старый твин если был
            flashTween?.Kill();
            // Сбрасываем параметр перед стартом
            iconImage.material.SetFloat("_FlashAmount", 0);
            // Зацикленное мигание (Yoyo)
            flashTween = iconImage.material.DOFloat(1f, "_FlashAmount", 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        // Анимация дыхания (масштаб)
        currentAnim?.Kill();
        currentAnim = transform.DOScale(0.85f, 1.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);

        manager.SpawnSleepParticles(this.GetComponent<RectTransform>());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Фидбек нажатия
        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f);

        if (currentState == State.Sleeping)
        {
            currentClicks++;
            transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.2f);

            if (currentClicks >= clicksNeeded)
                WakeUp(eventData.position);
        }
    }

    private void WakeUp(Vector2 clickPosition)
    {
        currentState = State.Active;

        // 1. ОСТАНАВЛИВАЕМ МИГАНИЕ И ВОЗВРАЩАЕМ МАТЕРИАЛ
        flashTween?.Kill();
        iconImage.material = defaultMaterial;

        // Награда (Джекпот)
        var saveManager = Object.FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            float randomMultiplier = Random.Range(2f, 4.1f);
            double totalWakeUpReward = clicksNeeded * saveManager.data.ClickPower * randomMultiplier;
            saveManager.data.Money += totalWakeUpReward;
            manager.SpawnFloatingNumber(totalWakeUpReward, clickPosition);
        }

        // Визуал пробуждения
        currentAnim?.Kill();
        transform.DOScale(1f, 0.2f);
        transform.DOPunchRotation(new Vector3(0, 0, 40f), 0.6f, 20).SetLink(gameObject);

        StartActiveAnimation();
    }

    private void StartActiveAnimation()
    {
        currentAnim?.Kill();
        currentAnim = transform.DOPunchRotation(new Vector3(0, 0, 5), Random.Range(3f, 5f), 1)
            .SetLoops(-1, LoopType.Restart)
            .SetDelay(Random.Range(0f, 2f))
            .SetLink(gameObject);
    }

    private void OnDestroy()
    {
        // Чистим всё
        transform.DOKill();
        flashTween?.Kill();

        var piManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
        if (piManager != null) piManager.UnregisterBuilding(this);
    }
}