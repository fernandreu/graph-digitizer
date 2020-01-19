using System;
using System.Globalization;
using System.Windows.Data;
using GraphDigitizer.Models;

namespace GraphDigitizer.Converters
{
    [ValueConversion(typeof(int), typeof(double))]
    public class FactorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int i))
            {
                return 1.0;
            }

            return Math.Pow(NumberUtils.ZoomFactor, i);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                return 0;
            }

            return Math.Log(d) / Math.Log(NumberUtils.ZoomFactor);
        }
    }
}
