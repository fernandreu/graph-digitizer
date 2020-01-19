using System;
using System.Globalization;
using System.Windows.Data;
using GraphDigitizer.Models;
using GraphDigitizer.Views;

namespace GraphDigitizer.Converters
{
    public class NumberFormatConverter : IValueConverter
    {
        public int ExponentialDecimals { get; set; } = 4;

        public int FloatDecimals { get; set; } = 8;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                return null;
            }

            return NumberUtils.FormatNum(d, this.ExponentialDecimals, this.FloatDecimals);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
