using System;
using System.Globalization;
using System.Windows.Data;

namespace GraphDigitizer.Converters
{
    [ValueConversion(typeof(double), typeof(double))]
    public class OffsetConverter : IValueConverter
    {
        public double Offset { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                d = 0.0;
            }

            return d + this.Offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                d = 0.0;
            }

            return d - this.Offset;
        }
    }
}
