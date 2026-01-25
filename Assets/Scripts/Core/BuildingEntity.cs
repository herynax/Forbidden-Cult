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
    [SerializeField] private Color sleepColor = new Color(0.5f, 0.5f, 1f);

    private Color originalColor;
    private Tween currentAnim;
    private BuildingVisualManager manager;
    private UpgradeSO myUpgrade;

    // Логика кликов
    private int clicksNeeded;
    private int currentClicks;

    public void Init(UpgradeSO upgrade, BuildingVisualManager mngr)
    {
        manager = mngr;
        myUpgrade = upgrade;
        originalColor = iconImage.color;

        // РЕГИСТРАЦИЯ: Добавляем себя в список менеджера доходов
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

        // Рандомим количество кликов для пробуждения из настроек SO
        clicksNeeded = Random.Range(myUpgrade.MinClicksToWake, myUpgrade.MaxClicksToWake + 1);
        currentClicks = 0;

        currentAnim.Kill();
        currentAnim = transform.DOScale(0.85f, 1.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        iconImage.DOColor(sleepColor, 1f);

        manager.SpawnSleepParticles(this.GetComponent<RectTransform>());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Всегда делаем "Панч" при клике, даже если не спит (фидбек)
        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f);

        if (currentState == State.Sleeping)
        {
            currentClicks++;

            // Визуальный эффект "сопротивления" (тряска)
            transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.2f);

            if (currentClicks >= clicksNeeded)
            {
                WakeUp();
            }
        }
    }

    private void WakeUp()
    {
        currentState = State.Active;

        iconImage.DOColor(originalColor, 0.5f);
        currentAnim.Kill();
        transform.DOScale(1f, 0.2f);
        StartActiveAnimation();

        // Эффект при окончательном пробуждении
        transform.DOPunchRotation(new Vector3(0, 0, 30f), 0.5f, 15);
    }

    private void StartActiveAnimation()
    {
        currentAnim = transform.DOPunchRotation(new Vector3(0, 0, 5), Random.Range(3f, 5f), 1)
            .SetLoops(-1, LoopType.Restart)
            .SetDelay(Random.Range(0f, 2f));
    }
}