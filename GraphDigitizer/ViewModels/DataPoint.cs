using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;
using GraphDigitizer.Views;

namespace GraphDigitizer.ViewModels
{
    public class DataPoint : ViewModelBase, ICanvasElement
    {
        private double x;

        /// <summary>
        /// The point's X coordinate in transformed units
        /// </summary>
        public double X
        {
            get => this.x;
            set => this.Set(ref this.x, value);
        }

        private double y;

        /// <summary>
        /// The point's Y coordinate in transformed units
        /// </summary>
        public double Y
        {
            get => this.y;
            set => this.Set(ref this.y, value);
        }

        private double realX;

        /// <summary>
        /// The point's X coordinate in screen units
        /// </summary>
        public double RealX
        {
            get => this.realX;
            set => this.Set(ref this.realX, value);
        }

        private double realY;

        /// <summary>
        /// The point's Y coordinate in screen units
        /// </summary>
        public double RealY
        {
            get => this.realY;
            set => this.Set(ref this.realY, value);
        }

        private int index;

        /// <summary>
        /// The index of the point as shown in the canvas
        /// </summary>
        public int Index
        {
            get => this.index;
            set => this.Set(ref this.index, value);
        }

        public string Xform { get; set; } //Formatted X and Y

        public string Yform { get; set; }

        public DataPoint(double x, double y, double realx, double realy, int pos)
        {
            this.X = x;
            this.Y = y;
            this.RealX = realx;
            this.RealY = realy;
            this.Xform = MainWindow.FormatNum(x);
            this.Yform = MainWindow.FormatNum(y);
            this.Index = pos;
        }
    }
}
