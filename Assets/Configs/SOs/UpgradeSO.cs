using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Clicker/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    public string ID;
    public string Name;
    public Sprite Icon;
    public Sprite RowBackground; // Уникальный фон для ряда построек
    public double BasePrice;
    public double BasePassiveIncome;

    [Header("Audio")]
    public EventReference PurchaseSound;

    [Header("Random Visuals")]
    public List<Color> possibleColors; // Список цветов, из которых будет выбираться рандомный
    public Material sleepMaterial;     // Материал, который накладывается во время сна

    [Header("Visual Layout")]
    public int RowsCount = 2;      // Сколько рядов будет в этой полосе
    public int IconsPerLine = 10;  // Сколько иконок влезет в один горизонтальный ряд
    public int MaxVisuals = 30;    // Лимит иконок (должен быть кратен RowsCount для красоты)

    [Header("Unlock Conditions")]
    public UpgradeSO RequiredUpgrade; // Какое улучшение нужно купить
    public int RequiredAmount;       // В каком количестве

    [Header("Sleep Mechanics")]
    public int MinClicksToWake = 3;
    public int MaxClicksToWake = 7;

    [Header("Tooltip Info")]
    [TextArea(3, 10)]
    public string Description; // "Увеличивает производство на..."
    [TextArea(2, 5)]
    public string LoreText;    // "Вкусные печеньки от доброй бабули"
}