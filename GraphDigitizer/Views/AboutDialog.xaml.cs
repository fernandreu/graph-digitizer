using System.Windows;
using GraphDigitizer.Attributes;
using GraphDigitizer.ViewModels;

namespace GraphDigitizer.Views
{
    [Register(typeof(AboutDialogViewModel))]
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
        }
    }
}
