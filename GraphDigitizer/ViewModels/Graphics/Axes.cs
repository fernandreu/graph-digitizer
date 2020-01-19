using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class Axes : ViewModelBase
    {
        public Axis X = new Axis { IsXAxis = true };

        public Axis Y = new Axis { IsXAxis = false };

        private int status;

        /// <summary>
        /// Which is the next point to assign, in the order above
        /// </summary>
        public int Status
        {
            get => this.status;
            set => this.Set(ref this.status, value);
        }

        private bool xLog;

        /// <summary>
        /// Whether the X axis is logarithmic or not
        /// </summary>
        public bool XLog
        {
            get => this.xLog;
            set => this.Set(ref this.xLog, value);
        }

        private bool yLog;

        /// <summary>
        /// Whether the Y axis is logarithmic or not
        /// </summary>
        public bool YLog
        {
            get => this.yLog;
            set => this.Set(ref this.yLog, value);
        }
    }
}
