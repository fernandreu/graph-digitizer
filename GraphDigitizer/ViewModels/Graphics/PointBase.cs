using GalaSoft.MvvmLight;

namespace GraphDigitizer.ViewModels.Graphics
{
    public abstract class PointBase : ViewModelBase
    {
        private double x;

        public double X
        {
            get => this.x;
            set => this.Set(ref this.x, value);
        }

        private double y;

        public double Y
        {
            get => this.y;
            set => this.Set(ref this.y, value);
        }
    }
}
