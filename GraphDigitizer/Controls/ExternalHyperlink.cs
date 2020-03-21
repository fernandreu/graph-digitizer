using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Command;

namespace GraphDigitizer.Controls
{
    /// <summary>
    /// Hyperlink which automatically takes care of opening its Uri in an external browser
    /// </summary>
    public class ExternalHyperlink : Hyperlink
    {
        public static readonly DependencyProperty OpenUrlCommandProperty = DependencyProperty.Register(
            nameof(OpenUrlCommand),
            typeof(RelayCommand<Uri>),
            typeof(ExternalHyperlink));

        public RelayCommand<Uri> OpenUrlCommand
        {
            get => (RelayCommand<Uri>)GetValue(OpenUrlCommandProperty);
            set => SetValue(OpenUrlCommandProperty, value);
        }

        public ExternalHyperlink()
        {
            // We give this the default value we will want in most cases while
            // still allowing it to be defined via xaml if needed
            OpenUrlCommand = new RelayCommand<Uri>(uri =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true,
                };

                Process.Start(psi);
            });

            RequestNavigate += OnRequestNavigate;
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenUrlCommand.Execute(e.Uri);
            e.Handled = true;
        }
    }
}
