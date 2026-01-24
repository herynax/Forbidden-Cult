using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class BuildingVisualManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private GameObject buildingPrefab;

    [Header("Sleep Effects")]
    public GameObject sleepUIPrefab;

    private Dictionary<string, Transform> activeRows = new Dictionary<string, Transform>();
    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    private void Start()
    {
        StartCoroutine(RestoreVisuals());
    }

    // Измененная сигнатура: теперь принимаем сохраненную позицию
    public void OnPurchase(UpgradeSO upgrade, int currentCount, bool isSilent = false, VisualPosition loadedPos = null)
    {
        if (!activeRows.ContainsKey(upgrade.ID))
        {
            CreateNewRow(upgrade, isSilent);
        }

        if (currentCount <= upgrade.MaxVisuals)
        {
            SpawnBuildingIcon(upgrade, currentCount, isSilent, loadedPos);
        }

        if (!isSilent) AnimateRow(upgrade.ID);
    }

    private void CreateNewRow(UpgradeSO upgrade, bool isSilent)
    {
        GameObject newRow = Instantiate(rowPrefab, transform);
        newRow.name = "Row_" + upgrade.ID;

        Image bg = newRow.GetComponent<Image>();
        if (bg != null && upgrade.RowBackground != null) bg.sprite = upgrade.RowBackground;

        activeRows.Add(upgrade.ID, newRow.transform);

        if (!isSilent)
        {
            newRow.transform.localScale = new Vector3(1, 0, 1);
            newRow.transform.DOScaleY(1, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void SpawnBuildingIcon(UpgradeSO upgrade, int currentCount, bool isSilent, VisualPosition loadedPos = null)
    {
        Transform rowTransform = activeRows[upgrade.ID];
        RectTransform rowRect = rowTransform.GetComponent<RectTransform>();

        GameObject icon = Instantiate(buildingPrefab, rowTransform);

        BuildingEntity entity = icon.GetComponent<BuildingEntity>();
        if (entity != null)
        {
            entity.Init(upgrade, this);
        }

        icon.GetComponent<Image>().sprite = upgrade.Icon;
        RectTransform iconRect = icon.GetComponent<RectTransform>();

        VisualPosition finalPos;

        if (loadedPos == null)
        {
            // --- ЛОГИКА ПООЧЕРЕДНОГО ЗАПОЛНЕНИЯ РЯДОВ ---

            float width = rowRect.rect.width;
            float height = rowRect.rect.height;

            // Расстояние между рядами и колонками
            float spacingY = height / (upgrade.RowsCount + 1);
            float spacingX = width / (upgrade.IconsPerLine + 1);

            // Индекс текущей покупки (от 0)
            int index = currentCount - 1;

            // Определяем РЯД (снизу вверх)
            // % upgrade.RowsCount заставляет индекс бегать: 0, 1, 2, 0, 1, 2...
            int rowIndex = index % upgrade.RowsCount;

            // Определяем КОЛОНКУ (слева направо)
            // / upgrade.RowsCount заставляет колонку меняться только после того, как заполнится вертикаль
            int colIndex = index / upgrade.RowsCount;

            // Считаем координаты
            // По Y: начинаем снизу (-height/2) и прибавляем шаг. 
            // По X: начинаем слева (-width/2) и прибавляем шаг.
            float baseX = (-width / 2f) + (colIndex + 1) * spacingX;
            float baseY = (-height / 2f) + (rowIndex + 1) * spacingY;

            // Добавляем Jitter (небольшой разброс)
            float jX = Random.Range(-spacingX * 0.15f, spacingX * 0.15f);
            float jY = Random.Range(-spacingY * 0.15f, spacingY * 0.15f);
            float jRot = Random.Range(-8f, 8f);

            finalPos = new VisualPosition(baseX + jX, baseY + jY, jRot);

            // Сохраняем в память
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgrade.ID);
            if (state != null) state.StoredPositions.Add(finalPos);
        }
        else
        {
            finalPos = loadedPos;
        }

        // Применяем позицию
        iconRect.anchoredPosition = new Vector2(finalPos.x, finalPos.y);
        iconRect.localRotation = Quaternion.Euler(0, 0, finalPos.zRotation);

        // Упорядочивание слоев (Z-order)
        // Чтобы нижние ряды отрисовывались ПОВЕРХ верхних (эффект перспективы), 
        // нужно менять SiblingIndex. В UI чем выше индекс, тем "ближе" объект.
        // Если rowIndex 0 (самый низ), он должен иметь самый большой индекс.
        // Но так как мы используем свободное наслоение, можно просто оставить как есть,
        // тогда новые постройки всегда будут чуть выше старых.

        icon.transform.DOPunchRotation(new Vector3(0, 0, 3), Random.Range(3f, 5f), 1)
            .SetLoops(-1, LoopType.Restart)
            .SetDelay(Random.Range(0f, 2f))
            .SetLink(icon);
    }

    private void AnimateRow(string upgradeID)
    {
        if (activeRows.TryGetValue(upgradeID, out Transform row))
        {
            row.DOKill(true);
            row.DOPunchScale(new Vector3(0.01f, 0.01f, 0), 0.2f);
        }
    }

    public void PulseRow(string upgradeID)
    {
        if (activeRows.TryGetValue(upgradeID, out Transform row))
        {
            // Мягкая пульсация масштаба всей группы
            row.DOKill(true);
            row.DOPunchScale(new Vector3(0.01f, 0.01f, 0), 0.2f);
        }
    }

    public void SpawnSleepParticles(RectTransform buildingRect)
    {
        // Спавним прямо ВНУТРЬ иконки персонажа
        GameObject effect = Instantiate(sleepUIPrefab, buildingRect);

        // Сбрасываем позицию в ноль, чтобы он был точно в центре иконки
        RectTransform effectRect = effect.GetComponent<RectTransform>();
        effectRect.anchoredPosition = Vector2.up * 50f; // Чуть выше головы

        // Уничтожаем через время
        Destroy(effect, 5f);
    }

    public System.Collections.IEnumerator RestoreVisuals()
    {
        yield return new WaitForEndOfFrame();
        var pm = FindFirstObjectByType<PassiveIncomeManager>();

        foreach (var upg in pm.allUpgrades)
        {
            var state = saveManager.data.Upgrades.Find(u => u.ID == upg.ID);
            if (state != null)
            {
                for (int i = 0; i < state.StoredPositions.Count; i++)
                {
                    // Передаем i + 1 как currentCount
                    OnPurchase(upg, i + 1, true, state.StoredPositions[i]);
                }
            }
        }
    }
}