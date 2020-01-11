using System;
using System.Globalization;
using System.Windows.Data;
using GraphDigitizer.Views;

namespace GraphDigitizer.Converters
{
    public class CoordinateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                return null;
            }

            return MainWindow.FormatNum(d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
