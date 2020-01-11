using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GraphDigitizer.Events;
using GraphDigitizer.Interfaces;
using GraphDigitizer.Models;

namespace GraphDigitizer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.SelectModeCommand = new RelayCommand(this.ExecuteSelectModeCommand);
            this.PointsModeCommand = new RelayCommand(this.ExecutePointsModeCommand);
            this.OpenFileCommand = new RelayCommand(this.ExecuteOpenFileCommand);

            this.Data.CollectionChanged += this.OnDataChanged;
        }

        private TargetImage targetImage;

        public TargetImage TargetImage
        {
            get => this.targetImage;
            set
            {
                var previous = this.targetImage;
                if (!this.Set(ref this.targetImage, value))
                {
                    return;
                }

                if (previous != null)
                {
                    this.CanvasElements.Remove(previous);
                }

                if (value != null)
                {
                    this.CanvasElements.Insert(0, value);
                }
            }
        }

        public ObservableCollection<DataPoint> Data { get; } = new ObservableCollection<DataPoint>();

        public ObservableCollection<ICanvasElement> CanvasElements { get; } = new ObservableCollection<ICanvasElement>();

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

        private int canvasFactor;

        public int CanvasFactor
        {
            get => this.canvasFactor;
            set => this.Set(ref this.canvasFactor, value);
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

        private Point realMousePosition;

        public Point RealMousePosition
        {
            get => this.realMousePosition;
            set => this.Set(ref this.realMousePosition, value);
        }

        private Point screenMousePosition;

        public Point ScreenMousePosition
        {
            get => this.screenMousePosition;
            set => this.Set(ref this.screenMousePosition, value);
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

        public RelayCommand OpenFileCommand { get; }

        public event EventHandler<FileEventArgs> OpeningFile;

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

        private void ExecuteOpenFileCommand()
        {
            var args = new FileEventArgs();
            this.OpeningFile?.Invoke(this, args);
            if (string.IsNullOrWhiteSpace(args.File))
            {
                return;
            }

            // TODO: Move code from MainWindow.OnOpenClicked
        }

        public (double, double) GetRealCoords(double screenX, double screenY)
        {
            var result = GetRealCoords(new Point(screenX, screenY));
            return (result.X, result.Y);
        }

        public Point GetRealCoords(Point screen)
        {
            //First: obtain the equivalent point in the X axis and in the Y axis
            var xAxis = -((Axes.Ymax.X - Axes.Ymin.X) * (Axes.Xmax.X * Axes.Xmin.Y - Axes.Xmax.Y * Axes.Xmin.X) - (Axes.Xmax.X - Axes.Xmin.X) * (screen.X * (Axes.Ymin.Y - Axes.Ymax.Y) + screen.Y * (Axes.Ymax.X - Axes.Ymin.X))) / ((Axes.Xmax.Y - Axes.Xmin.Y) * (Axes.Ymax.X - Axes.Ymin.X) - (Axes.Xmax.X - Axes.Xmin.X) * (Axes.Ymax.Y - Axes.Ymin.Y));
            var yAxis = (screen.Y * (Axes.Xmax.X - Axes.Xmin.X) * (Axes.Ymax.Y - Axes.Ymin.Y) + (Axes.Xmax.Y - Axes.Xmin.Y) * (Axes.Ymax.Y * Axes.Ymin.X - Axes.Ymax.X * Axes.Ymin.Y + screen.X * (Axes.Ymin.Y - Axes.Ymax.Y))) / ((Axes.Xmin.Y - Axes.Xmax.Y) * (Axes.Ymax.X - Axes.Ymin.X) + (Axes.Xmax.X - Axes.Xmin.X) * (Axes.Ymax.Y - Axes.Ymin.Y));

            var result = new Point();
            if (Axes.XLog)
            {
                result.X = Math.Pow(10.0, Math.Log10(Axes.Xmin.Value) + (xAxis - Axes.Xmin.X) / (Axes.Xmax.X - Axes.Xmin.X) * (Math.Log10(Axes.Xmax.Value) - Math.Log10(Axes.Xmin.Value)));
            }
            else
            {
                result.X = Axes.Xmin.Value + (xAxis - Axes.Xmin.X) / (Axes.Xmax.X - Axes.Xmin.X) * (Axes.Xmax.Value - Axes.Xmin.Value);
            }

            if (Axes.YLog)
            {
                result.Y = Math.Pow(10.0, Math.Log10(Axes.Ymin.Value) + (yAxis - Axes.Ymin.Y) / (Axes.Ymax.Y - Axes.Ymin.Y) * (Math.Log10(Axes.Ymax.Value) - Math.Log10(Axes.Ymin.Value)));
            }
            else
            {
                result.Y = Axes.Ymin.Value + (yAxis - Axes.Ymin.Y) / (Axes.Ymax.Y - Axes.Ymin.Y) * (Axes.Ymax.Value - Axes.Ymin.Value);
            }

            return result;
        }

        public void UpdateStatusCoords(double x, double y)
        {
            ScreenMousePosition = new Point(x, y);
            RealMousePosition = GetRealCoords(ScreenMousePosition);
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

        private void OnDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ICanvasElement>())
                {
                    this.CanvasElements.Remove(item);
                }
            }

            // TODO: This will add them at the top, but we want to preserve the same order as in Data
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ICanvasElement>())
                {
                    this.CanvasElements.Add(item);
                }
            }
        }
    }
}
