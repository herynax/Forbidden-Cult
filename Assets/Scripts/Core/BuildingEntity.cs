using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using FMODUnity;

public class BuildingEntity : MonoBehaviour, IPointerClickHandler
{
    public enum State { Active, Sleeping }
    public State currentState = State.Active;

    [Header("Visual Settings")]
    [SerializeField] private Image iconImage;
    private Material defaultMaterial;
    private Color myRandomColor;
    [SerializeField] private Color sleepColor = new Color(0.5f, 0.5f, 1f);

    private Color originalColor;
    private Tween currentAnim;
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
        defaultMaterial = iconImage.material; // Запоминаем стандартный материал

        // ЛОГИКА ЦВЕТА
        if (savedColor == default || savedColor.a == 0) // Если новый спавн
        {
            if (upgrade.possibleColors != null && upgrade.possibleColors.Count > 0)
            {
                myRandomColor = upgrade.possibleColors[Random.Range(0, upgrade.possibleColors.Count)];
            }
            else
            {
                myRandomColor = Color.white;
            }
        }
        else // Если загрузка из сейва
        {
            myRandomColor = savedColor;
        }

        iconImage.color = myRandomColor;

        // Регистрация в PassiveIncomeManager (как делали раньше)
        var piManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
        if (piManager != null) piManager.RegisterBuilding(this);

        StartActiveAnimation();
        InvokeRepeating(nameof(TryToSleep), Random.Range(10f, 20f), 15f);
    }


    public double GetIncomeValue()
    {
        return (myUpgrade != null) ? myUpgrade.BasePassiveIncome : 0;
    }

    public string GetUpgradeID()
    {
        return (myUpgrade != null) ? myUpgrade.ID : "";
    }

    private void OnDestroy()
    {
        transform.DOKill();
        // УДАЛЯЕМ СЕБЯ из списка при уничтожении объекта (например, при смене сцены)
        var piManager = Object.FindFirstObjectByType<PassiveIncomeManager>();
        if (piManager != null) piManager.UnregisterBuilding(this);
    }

    public double GetPassiveIncome()
    {
        // Если постройка еще не инициализирована, дохода нет
        if (myUpgrade == null) return 0;
        return myUpgrade.BasePassiveIncome;
    }

    private void TryToSleep()
    {
        if (currentState == State.Sleeping) return;

        if (Random.value < 0.1f) // 10% шанс уснуть
        {
            GoToSleep();
        }
    }

    private void GoToSleep()
    {
        currentState = State.Sleeping;
        clicksNeeded = Random.Range(myUpgrade.MinClicksToWake, myUpgrade.MaxClicksToWake + 1);
        currentClicks = 0;

        // МЕНЯЕМ МАТЕРИАЛ НА СОННЫЙ
        if (myUpgrade.sleepMaterial != null)
        {
            iconImage.material = myUpgrade.sleepMaterial;
        }

        currentAnim.Kill();
        currentAnim = transform.DOScale(0.85f, 1.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        manager.SpawnSleepParticles(this.GetComponent<RectTransform>());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RuntimeManager.PlayOneShot("event:/UI/Click");
        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f);

        if (currentState == State.Sleeping)
        {
            currentClicks++;

            // Визуальный эффект "сопротивления"
            transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.2f);

            // Если это был последний нужный клик
            if (currentClicks >= clicksNeeded)
            {
                // Передаем координаты клика в метод пробуждения для спавна цифр
                WakeUp(eventData.position);
            }
            else
            {
                // Можно спавнить маленькую цифру за промежуточный клик (опционально)
                // manager.SpawnFloatingNumber(saveManager.data.ClickPower, eventData.position);
            }
        }
    }

    private void WakeUp(Vector2 clickPosition)
    {
        currentState = State.Active;

        // ВОЗВРАЩАЕМ ДЕФОЛТНЫЙ МАТЕРИАЛ
        iconImage.material = defaultMaterial;

        // Логика награды (как ты просил в прошлом вопросе)
        var saveManager = Object.FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            float randomMultiplier = Random.Range(2f, 4.1f);
            double totalWakeUpReward = clicksNeeded * saveManager.data.ClickPower * randomMultiplier;
            saveManager.data.Money += totalWakeUpReward;
            manager.SpawnFloatingNumber(totalWakeUpReward, clickPosition);
        }

        currentAnim.Kill();
        transform.DOScale(1f, 0.2f);
        transform.DOPunchRotation(new Vector3(0, 0, 40f), 0.6f, 20);
        StartActiveAnimation();
    }

    private void StartActiveAnimation()
    {
        currentAnim = transform.DOPunchRotation(new Vector3(0, 0, 5), Random.Range(3f, 5f), 1)
            .SetLoops(-1, LoopType.Restart)
            .SetDelay(Random.Range(0f, 2f));
    }
}