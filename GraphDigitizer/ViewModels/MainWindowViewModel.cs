using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GraphDigitizer.Models;
using GraphDigitizer.Views;

namespace GraphDigitizer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.SelectModeCommand = new RelayCommand(this.ExecuteSelectModeCommand);
            this.PointsModeCommand = new RelayCommand(this.ExecutePointsModeCommand);
        }

        private Axes axes = new Axes();

        public Axes Axes
        {
            get => this.axes;
            set => this.Set(ref this.axes, value);
        }

        private int zoom = Properties.Settings.Default.Zoom;

        public int Zoom
        {
            get => this.zoom;
            set => this.Set(ref this.zoom, value);
        }

        private State state = State.Idle;

        public State State
        {
            get => this.state;
            set => this.Set(ref this.state, value);
        }

        private string statusText = string.Empty;

        /// <summary>
        /// The text to be shown in the status bar at the bottom
        /// </summary>
        public string StatusText
        {
            get => this.statusText;
            set => this.Set(ref this.statusText, value);
        }

        private Cursor canvasCursor = Cursors.Cross;

        /// <summary>
        /// The cursor to be shown the the mouse is over the Canvas control
        /// </summary>
        public Cursor CanvasCursor
        {
            get => this.canvasCursor;
            set => this.Set(ref this.canvasCursor, value);
        }

        public RelayCommand SelectModeCommand { get; }

        public RelayCommand PointsModeCommand { get; }

        private void ExecuteSelectModeCommand()
        {
            this.State = State.Select;
            this.SetToolTip();
            this.CanvasCursor = Cursors.Arrow;
        }

        private void ExecutePointsModeCommand()
        {
            this.State = State.Points;
            this.SetToolTip();
            this.CanvasCursor = Cursors.Cross;
        }

        public void SetToolTip()
        {
            switch (this.State)
            {
                case State.Idle:
                    this.StatusText = "Load an image with the button above";
                    break;
                case State.Axes:
                    switch (this.Axes.Status)
                    {
                        case 0:
                            this.StatusText = "Select the minor X value of the axes";
                            break;
                        case 1:
                            this.StatusText = "Select the major X value of the axes";
                            break;
                        case 2:
                            this.StatusText = "Select the minor Y value of the axes";
                            break;
                        case 3:
                            this.StatusText = "Select the major Y value of the axes";
                            break;
                    }
                    break;
                case State.Points:
                    this.StatusText = "Select any point you want to add to the data";
                    break;
                case State.Select:
                    this.StatusText = "Click on any point to select it";
                    break;
                default:
                    this.StatusText = string.Empty;
                    break;
            }
        }
    }
}
