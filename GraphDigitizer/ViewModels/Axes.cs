using System.Windows.Shapes;
using GalaSoft.MvvmLight;
using GraphDigitizer.ViewModels;

namespace GraphDigitizer.Models
{
    public class Axes : ViewModelBase
    {
        public Coord Xmin { get; } = new Coord { Value = 0 };
        
        public Coord Xmax { get; } = new Coord { Value = 1 };
        
        public Coord Ymin { get; } = new Coord { Value = 0 };
        
        public Coord Ymax { get; } = new Coord { Value = 1 };

        private int status;

        /// <summary>
        /// Which is the next point to assign, in the order above
        /// </summary>
        public int Status
        {
            get => this.status;
            set => this.Set(ref this.status, value);
        }

        private bool xlog;

        /// <summary>
        /// Whether the X axis is logarithmic or not
        /// </summary>
        public bool XLog
        {
            get => this.xlog;
            set => this.Set(ref this.xlog, value);
        }

        private bool ylog;

        /// <summary>
        /// Whether the Y axis is logarithmic or not
        /// </summary>
        public bool YLog
        {
            get => this.ylog;
            set => this.Set(ref this.ylog, value);
        }

        private Line xaxis;

        public Line Xaxis
        {
            get => this.xaxis;
            set => this.Set(ref this.xaxis, value);
        }
        
        private Line yaxis;

        public Line Yaxis
        {
            get => this.yaxis; 
            set => this.Set(ref this.yaxis, value);
        }
    }
}
