using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        public Help()
        {
            this.InitializeComponent();
            this.LoadPage(Properties.Resources.HelpGeneral);
        }

        private void OnGeneralTabGotFocus(object sender, RoutedEventArgs e)
        {
            this.LoadPage(Properties.Resources.HelpGeneral);
        }

        private void OnKeysTabGotFocus(object sender, RoutedEventArgs e)
        {
            this.LoadPage(Properties.Resources.HelpKeys);
        }

        private void OnAboutTabGotFocus(object sender, RoutedEventArgs e)
        {
            this.LoadPage(Properties.Resources.HelpAbout);
        }

        private void LoadPage(string rtf)
        {
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(rtf)), DataFormats.Rtf);
        }
    }
}
