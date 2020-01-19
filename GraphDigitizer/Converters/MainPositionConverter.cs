using System;
using System.Globalization;
using System.Windows.Data;

namespace GraphDigitizer.Converters
{
    /// <summary>
    /// Converter used to determine the location of each graphical element relative to the main canvas
    /// </summary>
    public class MainPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // The values should be, in this order:
            // - The relative position of the element
            // - The standard image size (before zoom is applied)
            // - The scale transform of the zoom image (i.e. zoom factor already converted)
            if (values.Length < 3)
            {
                throw new InvalidOperationException("Need to specify, in order: relative position, image size, zoom scale");
            }

            try
            {
                var position = (double) values[0];
                var imageSize = (double) values[1];
                var zoomScale = (double) values[2];

                var zoomSize = imageSize * zoomScale;
                return position * zoomSize;
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
