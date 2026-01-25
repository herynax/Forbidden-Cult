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
        if (value < 0.01) return "0";

        // 1. Если число меньше 1,000 — показываем максимум 2 знака после точки
        if (value < 1000)
        {
            // "F2" заставит всегда показывать 2 знака (например 5.12)
            // Если хочешь, чтобы целые числа были без нулей (просто 5), используй "0.##"
            return value.ToString("0.##", Style);
        }

        // 2. Если число от 1,000 до 1,000,000 — показываем целым с запятыми
        // Пример: 150,000 или 1,234
        if (value < 1000000)
        {
            return value.ToString("N0", Style);
        }

        // 3. Если число больше миллиона — сокращаем (M, B, T) + 2 знака
        int n = 0;
        double displayValue = value;
        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        return displayValue.ToString("F2", Style) + " " + Names[n];
    }
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