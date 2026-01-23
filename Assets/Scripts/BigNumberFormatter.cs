public static class BigNumberFormatter
{
    private static readonly string[] Names = { "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp" };

    public static string Format(double value)
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