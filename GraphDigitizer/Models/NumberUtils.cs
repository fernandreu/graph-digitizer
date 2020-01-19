using System;

namespace GraphDigitizer.Models
{
    public static class NumberUtils
    {
        public const double ZoomFactor = 1.2;

        public static string FormatNum(double num, int exponentialDecimals = 4, int floatDecimals = 8)
        {
            if (double.IsNaN(num))
            {
                return "N/A";
            }

            if (Math.Abs(num) < 1e-10)
            {
                return "0";
            }

            var aux = Math.Abs(num);
            if (aux > Math.Pow(10, exponentialDecimals + 2) - 1 || aux < Math.Pow(10, -exponentialDecimals - 1))
            {
                return num.ToString($"E{exponentialDecimals}");
            }

            var dig = (int)(Math.Log10(aux) + Math.Sign(Math.Log10(aux)));
            return num.ToString(dig >= 0 ? $"F{floatDecimals - dig}" : $"F{floatDecimals}");
        }
    }
}
