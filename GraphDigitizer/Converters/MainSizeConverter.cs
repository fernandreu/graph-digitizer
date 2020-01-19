using System;
using System.Globalization;
using System.Windows.Data;

namespace GraphDigitizer.Converters
{
    /// <summary>
    /// Converter used to determine the location of each graphical element relative to the main canvas
    /// </summary>
    public class MainSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // The values should be, in this order:
            // - The standard image size (before zoom is applied)
            // - The scale transform of the image (i.e. zoom factor already converted)
            if (values.Length < 2)
            {
                throw new InvalidOperationException("Need to specify, in order: image size, zoom scale");
            }

            try
            {
                var imageSize = (double) values[0];
                var zoomScale = (double) values[1];

                return imageSize * zoomScale;
            }
            catch (InvalidCastException)
            {
                // Perhaps the image was not set yet
                return 0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
