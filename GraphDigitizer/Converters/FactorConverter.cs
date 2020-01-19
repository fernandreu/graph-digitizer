﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace GraphDigitizer.Converters
{
    [ValueConversion(typeof(int), typeof(double))]
    public class FactorConverter : IValueConverter
    {
        public const double DefaultFactor = 1.2;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int i))
            {
                return 1.0;
            }

            return Math.Pow(DefaultFactor, i);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double d))
            {
                return 0;
            }

            return Math.Log(d) / Math.Log(DefaultFactor);
        }
    }
}