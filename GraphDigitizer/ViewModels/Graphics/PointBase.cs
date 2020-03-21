using GalaSoft.MvvmLight;

namespace GraphDigitizer.ViewModels.Graphics
{
    public abstract class PointBase : ViewModelBase
    {
        private double x;

        public double X
        {
            get => x;
            set => Set(ref x, value);
        }

        private double y;

        public double Y
        {
            get => y;
            set => Set(ref y, value);
        }
    }
}
