using System.Windows.Media;
using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels
{
    public class TargetImage : ViewModelBase, ICanvasElement
    {
        private ImageSource source;

        public ImageSource Source
        {
            get => source;
            set => Set(ref source, value);
        }

        public double Width { get; set; }

        public double Height { get; set; }
    }
}
