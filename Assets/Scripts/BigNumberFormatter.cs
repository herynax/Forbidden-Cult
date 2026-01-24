using System.Globalization;
using UnityEngine;

public static class BigNumberFormatter
{
    // Суффиксы для больших чисел
    private static readonly string[] Names = { "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp" };

    // Культура ru-RU: формат 1 234 567,890
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static string Format(double value)
    {
        if (value < 0.001) return "0,000";

        // Если число меньше миллиона, показываем его полностью с разделителями тысяч
        // Пример: 123 456,789
        if (value < 1000000)
        {
            return value.ToString("N3", RussianCulture);
        }

        // Если число больше миллиона, переходим к сокращениям (M, B, T...)
        int n = 0;
        double displayValue = value;

        // Делим, пока не дойдем до нужного суффикса
        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        // Форматируем остаток с 3 знаками после запятой
        // Пример: 1,234 M
        return displayValue.ToString("F3", RussianCulture) + " " + Names[n];
    }

    // Формат для магазина (цены обычно без 3 знаков после запятой, чтобы не забивать UI)
    public static string StoreFormat(double value)
    {
        if (value < 1000) return value.ToString("F0", RussianCulture);

        int n = 0;
        double displayValue = value;
        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        // Для цен в магазине оставим 2 знака, если есть суффикс
        // Пример: 1,25 k
        return displayValue.ToString("F2", RussianCulture) + " " + Names[n];
    }
}