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

    [Header("Click Settings")]
    [SerializeField] private GameObject moneyNumberPrefab; // Тот же префаб, что в кликере
    [SerializeField] private Transform canvasTransform;

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

    public void SpawnFloatingNumber(double amount, Vector2 screenPos)
    {
        if (moneyNumberPrefab == null) return;

        // Спавним через пул
        GameObject go = Lean.Pool.LeanPool.Spawn(moneyNumberPrefab, screenPos, Quaternion.identity, canvasTransform);
        var floatingNum = go.GetComponent<FloatingNumber>();
        if (floatingNum != null)
        {
            floatingNum.Initialize(amount, screenPos);
        }
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
        // 1. Поиск ряда
        if (!activeRows.TryGetValue(upgrade.ID, out Transform rowTransform)) return;
        RectTransform rowRect = rowTransform.GetComponent<RectTransform>();

        // 2. Создание объекта
        GameObject icon = Instantiate(buildingPrefab, rowTransform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        BuildingEntity entity = icon.GetComponent<BuildingEntity>();
        Image iconImage = icon.GetComponent<Image>();

        // 3. Определение цвета (сохраненный или новый рандомный)
        Color finalColor = Color.white;
        if (loadedPos != null)
        {
            finalColor = loadedPos.GetColor();
        }
        else if (upgrade.possibleColors != null && upgrade.possibleColors.Count > 0)
        {
            finalColor = upgrade.possibleColors[Random.Range(0, upgrade.possibleColors.Count)];
        }

        // 4. Инициализация логики здания
        if (entity != null)
        {
            entity.Init(upgrade, this, finalColor);
        }

        // 5. Визуальная настройка
        if (iconImage != null)
        {
            iconImage.sprite = upgrade.Icon;
            iconImage.color = finalColor;
        }

        // 6. Логика позиции (Jittered Grid)
        VisualPosition posData;

        if (loadedPos == null) // Новая покупка
        {
            float width = rowRect.rect.width;
            float height = rowRect.rect.height;

            // Расстояние между ячейками
            float spacingY = height / (upgrade.RowsCount + 1);
            float spacingX = width / (upgrade.IconsPerLine + 1);

            int index = currentCount - 1;
            int rowIndex = index % upgrade.RowsCount;
            int colIndex = index / upgrade.RowsCount;

            // Базовая позиция сетки
            float baseX = (-width / 2f) + (colIndex + 1) * spacingX;
            float baseY = (-height / 2f) + (rowIndex + 1) * spacingY;

            // Рандомизация (Jitter)
            float jX = Random.Range(-spacingX * 0.15f, spacingX * 0.15f);
            float jY = Random.Range(-spacingY * 0.15f, spacingY * 0.15f);
            float jRot = Random.Range(-8f, 8f);

            // Создаем данные позиции (включая выбранный цвет для сохранения)
            posData = new VisualPosition(baseX + jX, baseY + jY, jRot, finalColor);

            // Записываем в GameData
            var state = saveManager.data.Upgrades.Find(u => u.ID == upgrade.ID);
            if (state != null) state.StoredPositions.Add(posData);
        }
        else // Загрузка из сейва
        {
            posData = loadedPos;
        }

        // Применяем трансформации
        iconRect.anchoredPosition = new Vector2(posData.x, posData.y);
        iconRect.localRotation = Quaternion.Euler(0, 0, posData.zRotation);

        // Новые иконки всегда выше старых
        iconRect.SetAsLastSibling();

        // 7. Анимации
        if (!isSilent)
        {
            // Эффект появления "выпрыгивание"
            iconRect.localScale = Vector3.zero;
            iconRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetLink(icon);
        }
        else
        {
            iconRect.localScale = Vector3.one;
        }

        // Постоянное легкое покачивание (Idle)
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
        yield return new WaitForEndOfFrame();

        var pm = Object.FindFirstObjectByType<PassiveIncomeManager>();
        var sm = Object.FindFirstObjectByType<SaveManager>();

        if (sm == null || sm.data == null) yield break;

        foreach (var upg in pm.allUpgrades)
        {
            var state = sm.data.Upgrades.Find(u => u.ID == upg.ID);
            if (state != null)
            {
                // Отрисовываем здания по сохраненным позициям
                for (int i = 0; i < state.StoredPositions.Count; i++)
                {
                    OnPurchase(upg, i + 1, true, state.StoredPositions[i]);
                }
            }
        }
    }
}