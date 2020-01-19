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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DataPoint)
            {
                return this.PointTemplate;
            }

            if (item is TargetImage)
            {
                return this.ImageTemplate;
            }

            if (item is Axis)
            {
                return this.AxisTemplate;
            }

            return null;
        }
    }
}
