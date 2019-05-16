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
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(Properties.Resources.HelpGeneral)), DataFormats.Rtf);
        }

        private void OnGeneralTabGotFocus(object sender, RoutedEventArgs e)
        {
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(Properties.Resources.HelpGeneral)), DataFormats.Rtf);
        }

        private void OnKeysTabGotFocus(object sender, RoutedEventArgs e)
        {
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(Properties.Resources.HelpKeys)), DataFormats.Rtf);
        }

        private void OnAboutTabGotFocus(object sender, RoutedEventArgs e)
        {
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(Properties.Resources.HelpAbout)), DataFormats.Rtf);
        }
    }
}
