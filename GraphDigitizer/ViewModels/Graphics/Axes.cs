using GalaSoft.MvvmLight;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class Axes : ViewModelBase
    {
        public Axis X { get; } = new Axis { IsXAxis = true };

        public Axis Y { get; } = new Axis { IsXAxis = false };

        private int status;

        /// <summary>
        /// Which is the next point to assign, in the order above
        /// </summary>
        public int Status
        {
            get => status;
            set => Set(ref status, value);
        }

        private bool xLog;

        /// <summary>
        /// Whether the X axis is logarithmic or not
        /// </summary>
        public bool XLog
        {
            get => xLog;
            set => Set(ref xLog, value);
        }

        private bool yLog;

        /// <summary>
        /// Whether the Y axis is logarithmic or not
        /// </summary>
        public bool YLog
        {
            get => yLog;
            set => Set(ref yLog, value);
        }
    }
}
