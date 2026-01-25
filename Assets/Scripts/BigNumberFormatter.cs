using System.Globalization;
using UnityEngine;

public static class BigNumberFormatter
{
    // Суффиксы для больших чисел
    private static readonly string[] Names = { "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp" };

    // Используем InvariantCulture для формата 1,234,567.89 (запятая - тысячи, точка - дробь)
    private static readonly CultureInfo Style = CultureInfo.InvariantCulture;

    public static string Format(double value)
    {
        if (value < 1) return "0";

        // Если число меньше миллиона — выводим как целое с разделителем запятой
        // Пример: 150,000 или 5,400 или 999,999
        if (value < 1000000)
        {
            return value.ToString("N0", Style);
        }

        // Если число больше или равно миллиону — сокращаем и добавляем 2 знака после точки
        int n = 0;
        double displayValue = value;

        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        // Формат F2 дает ровно 2 знака после точки
        // Пример: 1.25 M или 1,500.20 B
        return displayValue.ToString("F2", Style) + " " + Names[n];
    }

    // Формат для магазина (цены)
    public static string StoreFormat(double value)
    {
        // В магазине обычно не нужны дробные доли, если число маленькое
        if (value < 1000) return value.ToString("F0", Style);

        // Если цена уже с буквой (k, M...) — показываем 2 знака
        int n = 0;
        double displayValue = value;
        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        // Если есть любая буква (даже 'k') — показываем 2 знака
        if (n > 0)
        {
            return displayValue.ToString("F2", Style) + " " + Names[n];
        }

        return displayValue.ToString("N0", Style);
    }
}