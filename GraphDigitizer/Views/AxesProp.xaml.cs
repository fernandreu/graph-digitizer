using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphDigitizer.Models;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for AxesProp.xaml
    /// </summary>
    public partial class AxesProp : Window
    {
        public Axes Axes { get; }

        public AxesProp(Axes ax)
        {
            this.Axes = ax;
            this.InitializeComponent();
            this.XMinBox.Text = this.Axes.Xmin.Value.ToString(CultureInfo.InvariantCulture);
            this.XMaxBox.Text = this.Axes.Xmax.Value.ToString(CultureInfo.InvariantCulture);
            this.XLogBox.IsChecked = this.Axes.XLog;
            this.YMinBox.Text = this.Axes.Ymin.Value.ToString(CultureInfo.InvariantCulture);
            this.YMaxBox.Text = this.Axes.Ymax.Value.ToString(CultureInfo.InvariantCulture);
            this.YLogBox.IsChecked = this.Axes.YLog;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(this.XMinBox.Text, out var val))
            {
                this.Axes.Xmin.Value = val;
            }

            if (double.TryParse(this.XMaxBox.Text, out val))
            {
                this.Axes.Xmax.Value = val;
            }

            if (double.TryParse(this.YMinBox.Text, out val))
            {
                this.Axes.Ymin.Value = val;
            }

            if (double.TryParse(this.YMaxBox.Text, out val))
            {
                this.Axes.Ymax.Value = val;
            }

            this.Axes.XLog = (bool)this.XLogBox.IsChecked;
            this.Axes.YLog = (bool)this.YLogBox.IsChecked;
            this.Close();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.XMinBox.Focus();
        }
    }
}
