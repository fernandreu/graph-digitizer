using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class DataPoint : ViewModelBase, ICanvasElement
    {
        private RelativePoint relative;

        public RelativePoint Relative
        {
            get => relative;
            set => Set(ref relative, value);
        }

        private TransformedPoint transformed;

        public TransformedPoint Transformed
        {
            get => transformed;
            set => Set(ref transformed, value);
        }

        private int index;

        /// <summary>
        /// The index of the point as shown in the canvas
        /// </summary>
        public int Index
        {
            get => index;
            set => Set(ref index, value);
        }

        private bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set => Set(ref isSelected, value);
        }

        public DataPoint(TransformedPoint transformed, RelativePoint relative, int pos)
        {
            Transformed = transformed;
            Relative = relative;
            Index = pos;
        }
    }
}
