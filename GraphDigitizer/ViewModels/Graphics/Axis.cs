using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class Axis : ViewModelBase, ICanvasElement
    {
        private RelativePoint minimum = new RelativePoint();

        public RelativePoint Minimum
        {
            get => this.minimum;
            set => this.Set(ref this.minimum, value);
        }

        private RelativePoint maximum = new RelativePoint();

        public RelativePoint Maximum
        {
            get => this.maximum;
            set => this.Set(ref this.maximum, value);
        }

        public bool IsXAxis { get; set; }

        private double minimumValue = 0.0;

        public double MinimumValue
        {
            get => this.minimumValue;
            set => this.Set(ref this.minimumValue, value);
        }

        private double maximumValue = 1.0;

        public double MaximumValue
        {
            get => this.maximumValue;
            set => this.Set(ref this.maximumValue, value);
        }
    }
}
