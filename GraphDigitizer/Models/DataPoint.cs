using System.Windows;
using System.Windows.Controls;
using GraphDigitizer.Views;

namespace GraphDigitizer.Models
{
    public class DataPoint
    {
        public double X { get; set; } //The point in transformed coordinates

        public double Y { get; set; }

        public double RealX { get; set; } //The point in screen coordinates

        public double RealY { get; set; }

        public Label Obj { get; set; } //Label containing the point element on the graph

        public string Xform { get; set; } //Formatted X and Y

        public string Yform { get; set; }

        public DataPoint(double x, double y, double realx, double realy, Canvas owner, int proportion, int pos)
        {
            this.X = x;
            this.Y = y;
            this.RealX = realx;
            this.RealY = realy;
            this.Xform = MainWindow.FormatNum(x);
            this.Yform = MainWindow.FormatNum(y);
            this.Obj = new Label() { Content = pos % 100, Style = (Style)Application.Current.FindResource("PointStyle") };
            int index = owner.Children.Add(this.Obj);
            Canvas.SetLeft(owner.Children[index], realx * proportion / 100 - 8);
            Canvas.SetTop(owner.Children[index], realy * proportion / 100 - 8);
            this.Obj.Tag = this;
        }
    }
}
