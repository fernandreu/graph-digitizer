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
            get => this.axes;
            set => this.Set(ref this.axes, value);
        }

        public AxesPropViewModel()
        {
            this.AcceptCommand = new RelayCommand(this.ExecuteAcceptCommand);
        }

        public event EventHandler Closing;

        public RelayCommand AcceptCommand { get; }

        private void ExecuteAcceptCommand()
        {
            this.Closing?.Invoke(this, EventArgs.Empty);
        }

        public void OnDataPassed(Axes data)
        {
            this.Axes = data;
        }
    }
}
