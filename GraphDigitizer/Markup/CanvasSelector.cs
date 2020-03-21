using System.Windows;
using System.Windows.Controls;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;
using GraphDigitizer.ViewModels.Graphics;

namespace GraphDigitizer.Markup
{
    public class CanvasSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate PointTemplate { get; set; }

        public DataTemplate AxisTemplate { get; set; }

        public DataTemplate SelectionRectangleTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DataPoint)
            {
                return PointTemplate;
            }

            if (item is TargetImage)
            {
                return ImageTemplate;
            }

            if (item is Axis)
            {
                return AxisTemplate;
            }

            if (item is Rectangle)
            {
                return SelectionRectangleTemplate;
            }

            return null;
        }
    }
}
