using System.Windows.Shapes;
using GraphDigitizer.Views;

namespace GraphDigitizer.Models
{
    public class Axes
    {
        public Coord Xmin, Xmax, Ymin, Ymax;
        public int Status; //Which is the next point to assign, in the order above
        public bool XLog, YLog; //If the axes are logarithmic or not
        public Line Xaxis, Yaxis;
    }
}
