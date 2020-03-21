using System;
using GraphDigitizer.Converters;
using GraphDigitizer.Models;

namespace GraphDigitizer.ViewModels.Graphics
{
    /// <summary>
    /// A point whose coordinates are in screen's rendered coordinates
    /// </summary>
    public class AbsolutePoint : PointBase
    {
        public AbsolutePoint()
        {
        }

        public AbsolutePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public RelativePoint ToRelative(double width, double height, int scaleFactor = 0)
        {
            var factor = Math.Pow(NumberUtils.ZoomFactor, scaleFactor);
            return new RelativePoint(X / (width * factor), Y / (height * factor));
        }
    }
}
