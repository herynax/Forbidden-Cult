using System.Collections.Generic;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public UpgradeSO[] allUpgrades;
    public GameObject buttonPrefab;
    public Transform container;
    public SaveManager saveManager;

    private Dictionary<string, UpgradeButton> spawnedButtons = new Dictionary<string, UpgradeButton>();

    void Start()
    {
        RefreshAvailableUpgrades();
    }

    public void RefreshAvailableUpgrades()
    {
        foreach (var upg in allUpgrades)
        {
            // Если кнопка уже есть, пропускаем
            if (spawnedButtons.ContainsKey(upg.ID)) continue;

            // Проверяем условия открытия
            bool canShow = false;
            if (upg.RequiredUpgrade == null)
            {
                canShow = true; // Начальные улучшения
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
            }
        }
    }
}