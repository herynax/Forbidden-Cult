using System.Globalization;

public static class BigNumberFormatter
{
    private static readonly string[] Names = { "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp" };

    public static string Format(double value)
    {
        // Если значение слишком маленькое, просто возвращаем 0 с дробью
        if (value < 0.001) return "0,000";

        int n = 0;
        double displayValue = value;

        while (displayValue >= 1000 && n < Names.Length - 1)
        {
            n++;
            displayValue /= 1000;
        }

        // Используем "F3" для 3 знаков после запятой
        // Если хочешь именно запятую как разделитель (4,123), используй культуру
        return displayValue.ToString("F3", CultureInfo.GetCultureInfo("ru-RU")) + Names[n];
    }

    public static string StoreFormat(double value)
    {
        if (value < 1000) return value.ToString("F0");

        int n = 0;
        while (value >= 1000 && n < Names.Length - 1)
        {
            n++;
            value /= 1000;
        }
        return value.ToString("F2") + Names[n];
    }
}