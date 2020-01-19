using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels.Graphics;

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
            this.XMinBox.Text = this.Axes.X.MinimumValue.ToString(CultureInfo.InvariantCulture);
            this.XMaxBox.Text = this.Axes.X.MaximumValue.ToString(CultureInfo.InvariantCulture);
            this.XLogBox.IsChecked = this.Axes.XLog;

            this.YMinBox.Text = this.Axes.Y.MinimumValue.ToString(CultureInfo.InvariantCulture);
            this.YMaxBox.Text = this.Axes.Y.MaximumValue.ToString(CultureInfo.InvariantCulture);
            this.YLogBox.IsChecked = this.Axes.YLog;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(this.XMinBox.Text, out var val))
            {
                this.Axes.X.MinimumValue = val;
            }

            if (double.TryParse(this.XMaxBox.Text, out val))
            {
                this.Axes.X.MaximumValue = val;
            }

            if (double.TryParse(this.YMinBox.Text, out val))
            {
                this.Axes.Y.MinimumValue = val;
            }

            if (double.TryParse(this.YMaxBox.Text, out val))
            {
                this.Axes.Y.MaximumValue = val;
            }

            this.Axes.XLog = this.XLogBox.IsChecked.Value;
            this.Axes.YLog = this.YLogBox.IsChecked.Value;
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
