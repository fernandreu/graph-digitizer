using System;
using GraphDigitizer.Converters;

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
            this.X = x;
            this.Y = y;
        }

        public RelativePoint ToRelative(double width, double height, int scaleFactor = 0)
        {
            var factor = Math.Pow(FactorConverter.DefaultFactor, scaleFactor);
            return new RelativePoint(this.X / (width * factor), this.Y / (height * factor));
        }
    }
}
