using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class Rectangle : ViewModelBase, ICanvasElement
    {
        private double top;

        public double Top
        {
            get => top;
            set => Set(ref top, value);
        }

        private double left;

        public double Left
        {
            get => left;
            set => Set(ref left, value);
        }

        private double width;

        public double Width
        {
            get => width;
            set => Set(ref width, value);
        }

        private double height;

        public double Height
        {
            get => height;
            set => Set(ref height, value);
        }
    }
}
