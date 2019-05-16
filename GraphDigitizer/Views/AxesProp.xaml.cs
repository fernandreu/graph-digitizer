using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for AxesProp.xaml
    /// </summary>
    public partial class AxesProp : Window
    {
        public Axes axes;

        public AxesProp(Axes ax)
        {
            this.InitializeComponent();
            this.axes = ax;
            this.XMinBox.Text = this.axes.Xmin.Value.ToString();
            this.XMaxBox.Text = this.axes.Xmax.Value.ToString();
            this.XLogBox.IsChecked = this.axes.XLog;
            this.YMinBox.Text = this.axes.Ymin.Value.ToString();
            this.YMaxBox.Text = this.axes.Ymax.Value.ToString();
            this.YLogBox.IsChecked = this.axes.YLog;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            double val;
            if (double.TryParse(this.XMinBox.Text, out val))
                this.axes.Xmin.Value = val;
            if (double.TryParse(this.XMaxBox.Text, out val))
                this.axes.Xmax.Value = val;
            if (double.TryParse(this.YMinBox.Text, out val))
                this.axes.Ymin.Value = val;
            if (double.TryParse(this.YMaxBox.Text, out val))
                this.axes.Ymax.Value = val;
            this.axes.XLog = (bool)this.XLogBox.IsChecked;
            this.axes.YLog = (bool)this.YLogBox.IsChecked;
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
