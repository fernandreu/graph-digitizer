namespace GraphDigitizer.ViewModels.Graphics
{
    /// <summary>
    /// A point whose coordinates are relative to the values defined by the axes, hence in
    /// transformed (real) space
    /// </summary>
    public class TransformedPoint : PointBase
    {
        public TransformedPoint()
        {
        }

        public TransformedPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
