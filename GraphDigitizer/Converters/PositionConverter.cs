using System;
using System.Globalization;
using System.Windows.Data;

namespace GraphDigitizer.Converters
{
    public class PositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // The values should be, in this order:
            // - The relative mouse position
            // - The main image size
            // - The zoom **canvas** size
            // - The zoom factor of the zoom image
            if (values.Length < 4)
            {
                throw new InvalidOperationException("Need to specify, in order: mouse position, main image size, canvas size, zoom factor");
            }

            try
            {
                var mousePosition = (double) values[0];
                var imageSize = (double) values[1];
                var canvasSize = (double) values[2];
                var zoomFactor = (double) values[3];

                var zoomSize = imageSize * zoomFactor;
                return 0.5 * canvasSize - mousePosition * zoomSize;
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
