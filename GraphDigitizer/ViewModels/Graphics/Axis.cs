using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class Axis : ViewModelBase, ICanvasElement
    {
        private RelativePoint minimum = new RelativePoint();

        public RelativePoint Minimum
        {
            get => minimum;
            set => Set(ref minimum, value);
        }

        private RelativePoint maximum = new RelativePoint();

        public RelativePoint Maximum
        {
            get => maximum;
            set => Set(ref maximum, value);
        }

        public bool IsXAxis { get; set; }

        private double minimumValue = 0.0;

        public double MinimumValue
        {
            get => minimumValue;
            set => Set(ref minimumValue, value);
        }

        private double maximumValue = 1.0;

        public double MaximumValue
        {
            get => maximumValue;
            set => Set(ref maximumValue, value);
        }
    }
}
