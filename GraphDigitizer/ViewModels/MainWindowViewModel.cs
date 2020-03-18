using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GraphDigitizer.Converters;
using GraphDigitizer.Events;
using GraphDigitizer.Interfaces;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels.Graphics;
using GraphDigitizer.Views;

namespace GraphDigitizer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.SelectModeCommand = new RelayCommand(this.ExecuteSelectModeCommand);
            this.PointsModeCommand = new RelayCommand(this.ExecutePointsModeCommand);
            this.OpenFileCommand = new RelayCommand(this.ExecuteOpenFileCommand);
            this.ClipboardLoadCommand = new RelayCommand(this.OpenFromClipboard);
            this.ShowHelpCommand = new RelayCommand(this.ExecuteShowHelpCommand);
            this.ZoomModeEnterCommand = new RelayCommand(this.ExecuteZoomModeEnterCommand);
            this.ZoomModeLeaveCommand = new RelayCommand(this.ExecuteZoomModeLeaveCommand);
            this.KeyUpCommand = new RelayCommand<KeyEventArgs>(this.ExecuteKeyUpCommand);

            this.Data.CollectionChanged += this.OnDataChanged;
            this.Axes = new Axes();
        }

        public RelayCommand SelectModeCommand { get; }

        public RelayCommand PointsModeCommand { get; }

        public RelayCommand OpenFileCommand { get; }

        public RelayCommand ClipboardLoadCommand { get; }

        public RelayCommand ShowHelpCommand { get; }

        public RelayCommand ZoomModeEnterCommand { get; }

        public RelayCommand ZoomModeLeaveCommand { get; }

        public RelayCommand<KeyEventArgs> KeyUpCommand { get; }

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

        private Axes axes;

        public Axes Axes
        {
            get => this.axes;
            set
            {
                var previous = this.axes;
                if (!this.Set(ref this.axes, value))
                {
                    return;
                }

                if (previous != null)
                {
                    this.CanvasElements.Remove(previous.X);
                    this.CanvasElements.Remove(previous.Y);
                }

                if (value != null)
                {
                    var offset = this.CanvasElements.Count > 0 ? 1 : 0;
                    this.CanvasElements.Insert(offset, value.X);
                    this.CanvasElements.Insert(offset + 1, value.Y);
                }
            }
        }

        private RelativePoint mousePosition;

        // The position of the mouse relative to the image's size, from 0 to 1
        public RelativePoint MousePosition
        {
            get => this.mousePosition;
            set
            {
                if (!this.Set(ref this.mousePosition, value))
                {
                    return;
                }

                this.UpdateStatusCoords();
            }
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

        private TransformedPoint realMousePosition;

        public TransformedPoint RealMousePosition
        {
            get => this.realMousePosition;
            set => this.Set(ref this.realMousePosition, value);
        }

        private AbsolutePoint screenMousePosition;

        public AbsolutePoint ScreenMousePosition
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

        private bool isMouseOverCanvas;

        public bool IsMouseOverCanvas
        {
            get => this.isMouseOverCanvas;
            set => this.Set(ref this.isMouseOverCanvas, value);
        }

        private bool isInZoomMode;

        public bool IsInZoomMode
        {
            get => this.isInZoomMode;
            set => this.Set(ref this.isInZoomMode, value);
        }

        public event EventHandler<FileEventArgs> OpeningFile;

        public event EventHandler ZoomModeEnter;

        public event EventHandler ZoomModeLeave;

        public event EventHandler<LaunchDialogEventArgs> DialogLaunch;

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

            this.OpenFile(args.File);
        }

        private void ExecuteShowHelpCommand()
        {
            this.LaunchDialog<AboutDialog>();
        }

        private void ExecuteZoomModeEnterCommand()
        {
            if (!this.IsMouseOverCanvas || this.IsInZoomMode)
            {
                return;
            }

            this.ZoomModeEnter?.Invoke(this, EventArgs.Empty);
            this.IsInZoomMode = true;
        }

        private void ExecuteZoomModeLeaveCommand()
        {
            this.IsInZoomMode = false;
            this.ZoomModeLeave?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteKeyUpCommand(KeyEventArgs e)
        {
            if (e.Key == Key.Z && this.IsInZoomMode)
            {
                this.ExecuteZoomModeLeaveCommand();
            }
        }

        private void OpenFromClipboard()
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("The Clipboard does not contain a valid image.", "Invalid Clipboard content", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bmp = Clipboard.GetImage();
            this.LoadBitmap(bmp);
        }

        public void OpenFile(string path)
        {
            var bmp = new BitmapImage(new Uri(path));
            this.LoadBitmap(bmp);
        }

        private void LoadBitmap(BitmapSource bmp)
        {
            //Since everything will be deleted, there is no need for calling UpdateProportions(100.0)
            this.CanvasFactor = 0;

            this.TargetImage = new TargetImage
            {
                Source = bmp,
                Width = bmp.PixelWidth,
                Height = bmp.PixelHeight
            };

            this.State = State.Axes;
            this.Axes.Status = 0;

            this.Data.Clear();
            Axes.X.Minimum.X = Axes.X.Minimum.Y = Axes.X.Maximum.X = Axes.X.Maximum.Y = Axes.Y.Minimum.X = Axes.Y.Minimum.Y = Axes.Y.Maximum.X = Axes.Y.Maximum.Y = double.NaN;

            SetToolTip();
            this.CanvasCursor = Cursors.Cross;
        }

        public TransformedPoint GetRealCoords(AbsolutePoint absolute)
        {
            return this.GetRealCoords(absolute.ToRelative(this.TargetImage.Width, this.TargetImage.Height, this.CanvasFactor));
        }

        public TransformedPoint GetRealCoords(RelativePoint relative)
        {
            //First: obtain the equivalent point in the X axis and in the Y axis
            var xAxis = -((Axes.Y.Maximum.X - Axes.Y.Minimum.X) * (Axes.X.Maximum.X * Axes.X.Minimum.Y - Axes.X.Maximum.Y * Axes.X.Minimum.X) - (Axes.X.Maximum.X - Axes.X.Minimum.X) * (relative.X * (Axes.Y.Minimum.Y - Axes.Y.Maximum.Y) + relative.Y * (Axes.Y.Maximum.X - Axes.Y.Minimum.X))) / ((Axes.X.Maximum.Y - Axes.X.Minimum.Y) * (Axes.Y.Maximum.X - Axes.Y.Minimum.X) - (Axes.X.Maximum.X - Axes.X.Minimum.X) * (Axes.Y.Maximum.Y - Axes.Y.Minimum.Y));
            var yAxis = (relative.Y * (Axes.X.Maximum.X - Axes.X.Minimum.X) * (Axes.Y.Maximum.Y - Axes.Y.Minimum.Y) + (Axes.X.Maximum.Y - Axes.X.Minimum.Y) * (Axes.Y.Maximum.Y * Axes.Y.Minimum.X - Axes.Y.Maximum.X * Axes.Y.Minimum.Y + relative.X * (Axes.Y.Minimum.Y - Axes.Y.Maximum.Y))) / ((Axes.X.Minimum.Y - Axes.X.Maximum.Y) * (Axes.Y.Maximum.X - Axes.Y.Minimum.X) + (Axes.X.Maximum.X - Axes.X.Minimum.X) * (Axes.Y.Maximum.Y - Axes.Y.Minimum.Y));

            var result = new TransformedPoint();
            if (Axes.XLog)
            {
                result.X = Math.Pow(10.0, Math.Log10(Axes.X.MinimumValue) + (xAxis - Axes.X.Minimum.X) / (Axes.X.Maximum.X - Axes.X.Minimum.X) * (Math.Log10(Axes.X.MaximumValue) - Math.Log10(Axes.X.MinimumValue)));
            }
            else
            {
                result.X = Axes.X.MinimumValue + (xAxis - Axes.X.Minimum.X) / (Axes.X.Maximum.X - Axes.X.Minimum.X) * (Axes.X.MaximumValue - Axes.X.MinimumValue);
            }

            if (Axes.YLog)
            {
                result.Y = Math.Pow(10.0, Math.Log10(Axes.Y.MinimumValue) + (yAxis - Axes.Y.Minimum.Y) / (Axes.Y.Maximum.Y - Axes.Y.Minimum.Y) * (Math.Log10(Axes.Y.MaximumValue) - Math.Log10(Axes.Y.MinimumValue)));
            }
            else
            {
                result.Y = Axes.Y.MinimumValue + (yAxis - Axes.Y.Minimum.Y) / (Axes.Y.Maximum.Y - Axes.Y.Minimum.Y) * (Axes.Y.MaximumValue - Axes.Y.MinimumValue);
            }

            return result;
        }

        public void UpdateStatusCoords(RelativePoint point = null)
        {
            if (point == null)
            {
                point = this.MousePosition;
            }

            this.ScreenMousePosition = point.ToAbsolute(this.TargetImage.Width, this.TargetImage.Height, this.CanvasFactor);
            this.RealMousePosition = GetRealCoords(ScreenMousePosition);
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
        
        public void AddPoint(RelativePoint position)
        {
            var transformed = this.GetRealCoords(position);
            var p = new DataPoint(transformed, position, this.Data.Count + 1);
            this.Data.Add(p);
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

        private void LaunchDialog<T>() where T : Window
        {
            this.DialogLaunch?.Invoke(this, new LaunchDialogEventArgs(typeof(T)));
        }
    }
}
