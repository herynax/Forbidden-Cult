using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public UpgradeSO[] allUpgrades;
    public GameObject buttonPrefab;
    public Transform container;
    private SaveManager saveManager;

    private Dictionary<string, UpgradeButton> spawnedButtons = new Dictionary<string, UpgradeButton>();

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
    }

    void Start()
    {
        RefreshAvailableUpgrades();
    }

    public void RefreshAvailableUpgrades()
    {

        foreach (var upg in allUpgrades)
        {
            // 1. Если кнопка уже создана - пропускаем
            if (spawnedButtons.ContainsKey(upg.ID)) continue;

            // 2. Проверяем условия
            bool canShow = false;
            if (upg.RequiredUpgrade == null)
            {
                canShow = true;
            }
            else
            {
                int count = saveManager.data.GetUpgradeCount(upg.RequiredUpgrade.ID);
                if (count >= upg.RequiredAmount) canShow = true;
            }

            if (canShow)
            {
                GameObject btnObj = Instantiate(buttonPrefab, container);
                UpgradeButton btn = btnObj.GetComponent<UpgradeButton>();
                btn.upgradeSO = upg;
                btn.Init(saveManager);
                spawnedButtons.Add(upg.ID, btn);

                // Принудительно обновляем Layout
                LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            }
        }
    }
}