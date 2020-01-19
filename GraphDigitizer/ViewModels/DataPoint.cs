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

        private double relativeX;

        /// <summary>
        /// The point's X coordinate in relative units (0 for left, 1 for right)
        /// </summary>
        public double RelativeX
        {
            get => this.relativeX;
            set => this.Set(ref this.relativeX, value);
        }

        private double relativeY;

        /// <summary>
        /// The point's Y coordinate in relative units (0 for top, 1 for bottom)
        /// </summary>
        public double RelativeY
        {
            get => this.relativeY;
            set => this.Set(ref this.relativeY, value);
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

        public DataPoint(double x, double y, double relativeX, double relativeY, int pos)
        {
            this.X = x;
            this.Y = y;
            this.RelativeX = relativeX;
            this.RelativeY = relativeY;
            this.Xform = MainWindow.FormatNum(x);
            this.Yform = MainWindow.FormatNum(y);
            this.Index = pos;
        }
    }
}
