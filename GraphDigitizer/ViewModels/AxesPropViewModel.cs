using System;
using System.Collections.Generic;
using System.Text;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GraphDigitizer.Interfaces;
using GraphDigitizer.ViewModels.Graphics;

namespace GraphDigitizer.ViewModels
{
    public class AxesPropViewModel : ViewModelBase, ICanPassData<Axes>
    {
        private Axes axes;

        public Axes Axes
        {
            get => axes;
            set => Set(ref axes, value);
        }

        public AxesPropViewModel()
        {
            AcceptCommand = new RelayCommand(ExecuteAcceptCommand);
        }

        public event EventHandler Closing;

        public RelayCommand AcceptCommand { get; }

        private void ExecuteAcceptCommand()
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }

        public void OnDataPassed(Axes data)
        {
            Axes = data;
        }
    }
}
