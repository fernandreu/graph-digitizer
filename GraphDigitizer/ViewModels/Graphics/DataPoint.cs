using GalaSoft.MvvmLight;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.ViewModels.Graphics
{
    public class DataPoint : ViewModelBase, ICanvasElement
    {
        private RelativePoint relative;

        public RelativePoint Relative
        {
            get => this.relative;
            set => this.Set(ref this.relative, value);
        }

        private TransformedPoint transformed;

        public TransformedPoint Transformed
        {
            get => this.transformed;
            set => this.Set(ref this.transformed, value);
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

        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.Set(ref this.isSelected, value);
        }

        public DataPoint(TransformedPoint transformed, RelativePoint relative, int pos)
        {
            this.Transformed = transformed;
            this.Relative = relative;
            this.Index = pos;
        }
    }
}
