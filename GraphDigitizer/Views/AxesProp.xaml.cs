using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphDigitizer.Attributes;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;

namespace GraphDigitizer.Views
{
    [Register(typeof(AxesPropViewModel))]
    public partial class AxesProp : Window
    {
        private AxesPropViewModel viewModel;

        public AxesProp()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property != DataContextProperty || !(DataContext is AxesPropViewModel vm))
            {
                return;
            }

            if (viewModel != null)
            {
                viewModel.Closing -= ClosingEventHandler;
            }

            viewModel = vm;
            vm.Closing += ClosingEventHandler;
        }

        private void ClosingEventHandler(object sender, EventArgs e)
        {
            Close();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            //Try to position the window leaving the mouse in a corner
            MouseUtils.GetCursorPos(out var p);
            if (p.X + Width > SystemParameters.PrimaryScreenWidth)
                Left = p.X - Width + 20;
            else
                Left = p.X;

            if (p.Y + Height > SystemParameters.PrimaryScreenHeight - 50) // Threshold for the Windows taskbar
                Top = p.Y - Height;
            else
                Top = p.Y;
        }
    }
}
