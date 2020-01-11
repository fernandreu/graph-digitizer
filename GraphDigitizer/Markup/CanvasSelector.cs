using System.Windows;
using System.Windows.Controls;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;

namespace GraphDigitizer.Markup
{
    public class CanvasSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate PointTemplate { get; set; }

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

            return null;
        }
    }
}
