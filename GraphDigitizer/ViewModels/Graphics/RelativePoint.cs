using System;
using GraphDigitizer.Converters;

namespace GraphDigitizer.ViewModels.Graphics
{
    /// <summary>
    /// A point whose coordinates are relative to the image size, with 0 being
    /// the top / left edge and 1 being bottom / right edge
    /// </summary>
    public class RelativePoint : PointBase
    {
        public RelativePoint()
        {
        }

        public RelativePoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public AbsolutePoint ToAbsolute(double width, double height, int scaleFactor = 0)
        {
            var factor = Math.Pow(FactorConverter.DefaultFactor, scaleFactor);
            return new AbsolutePoint(this.X * width * factor, this.Y * height * factor);
        }
    }
}
