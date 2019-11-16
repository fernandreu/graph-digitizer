using GalaSoft.MvvmLight;

namespace GraphDigitizer.ViewModels
{
    public class Coord : ViewModelBase
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

        private double value;

        public double Value
        {
            get => this.value;
            set => this.Set(ref this.value, value);
        }
    }
}
