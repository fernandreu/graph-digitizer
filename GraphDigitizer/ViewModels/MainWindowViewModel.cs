using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GraphDigitizer.Events;
using GraphDigitizer.Interfaces;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels.Graphics;
using Microsoft.Win32;

namespace GraphDigitizer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IDialogService dialogService;

        public MainWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            this.SaveCommand = new RelayCommand(this.ExecuteSaveCommand);
            this.AxesCommand = new RelayCommand(this.ExecuteAxesCommand);
            this.CopyCommand = new RelayCommand(this.ExecuteCopyCommand);
            this.ZoomInCommand = new RelayCommand(this.ExecuteZoomInCommand);
            this.ZoomOutCommand = new RelayCommand(this.ExecuteZoomOutCommand);
            this.EnlargeCommand = new RelayCommand(this.ExecuteEnlargeCommand);
            this.ReduceCommand = new RelayCommand(this.ExecuteReduceCommand);
            this.RestoreCommand = new RelayCommand(this.ExecuteRestoreCommand);
            this.ClearDataCommand = new RelayCommand(this.ExecuteClearDataCommand);
            this.AxesPropCommand = new RelayCommand(this.SelectAxesProp);
            this.SelectModeCommand = new RelayCommand(this.ExecuteSelectModeCommand);
            this.PointsModeCommand = new RelayCommand(this.ExecutePointsModeCommand);
            this.OpenFileCommand = new RelayCommand(this.ExecuteOpenFileCommand);
            this.ClipboardLoadCommand = new RelayCommand(this.OpenFromClipboard);
            this.ShowHelpCommand = new RelayCommand(this.ExecuteShowHelpCommand);
            this.ZoomModeEnterCommand = new RelayCommand(this.ExecuteZoomModeEnterCommand);
            this.ZoomModeLeaveCommand = new RelayCommand(this.ExecuteZoomModeLeaveCommand);
            this.KeyUpCommand = new RelayCommand<KeyEventArgs>(this.ExecuteKeyUpCommand);

            this.Data.CollectionChanged += this.OnDataChanged;
            this.SelectedData.CollectionChanged += this.OnSelectedDataChanged;
            this.Axes = new Axes();
        }

        public RelayCommand SaveCommand { get; }

        public RelayCommand AxesCommand { get; }

        public RelayCommand CopyCommand { get; }

        public RelayCommand ZoomInCommand { get; }

        public RelayCommand ZoomOutCommand { get; }

        public RelayCommand EnlargeCommand { get; }

        public RelayCommand ReduceCommand { get; }

        public RelayCommand RestoreCommand { get; }

        public RelayCommand ClearDataCommand { get; }

        public RelayCommand AxesPropCommand { get; }

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

        public ObservableCollection<DataPoint> SelectedData { get; } = new ObservableCollection<DataPoint>();

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

        public static BitmapImage ImageFromBuffer(byte[] bytes)
        {
            var stream = new System.IO.MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }

        public static byte[] BufferFromImage(BitmapImage imageSource)
        {
            var ms = new System.IO.MemoryStream();
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(imageSource));
            enc.Save(ms);
            return ms.ToArray();
        }

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
            this.dialogService.ShowDialog<AboutDialogViewModel>();
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
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in this.CanvasElements.OfType<DataPoint>().ToList())
                {
                    this.CanvasElements.Remove(item);
                }

                return;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<DataPoint>())
                {
                    this.CanvasElements.Remove(item);
                }
            }

            // TODO: This will add them at the top, but we want to preserve the same order as in Data
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<DataPoint>())
                {
                    this.CanvasElements.Add(item);
                }
            }
        }

        private void OnSelectedDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in this.Data)
                {
                    item.IsSelected = false;
                }

                return;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<DataPoint>())
                {
                    item.IsSelected = false;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<DataPoint>())
                {
                    item.IsSelected = true;
                }
            }
        }

        public void SelectAxesProp()
        {
            if (this.IsInZoomMode) this.ZoomModeLeaveCommand.Execute(null);
            var ap = this.dialogService.ShowDialog<AxesPropViewModel, Axes>(Axes);
            Axes = ap.Axes;
            this.State = State.Points;
        }

        public void HandlePointMouseDown(DataPoint point, MouseButton button)
        {
            if (State != State.Select)
            {
                return;
            }

            if (button == MouseButton.Left) //Select mode
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    // Add to current selection. If it was already selected, unselect
                    if (SelectedData.Contains(point))
                        SelectedData.Remove(point);
                    else
                        SelectedData.Add(point);
                }
                else if (SelectedData.Count == 1 && SelectedData.Contains(point))
                    SelectedData.Clear();
                else
                {
                    SelectedData.Clear();
                    SelectedData.Add(point);
                }
            }
            else if (button == MouseButton.Right) //Delete mode
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    DeleteSelection();
                else
                {
                    Data.Remove(point);
                    UpdateIndices();
                }
            }
        }

        public void DeleteSelection()
        {
            foreach (var point in SelectedData.ToList())
            {
                SelectedData.Remove(point);
                Data.Remove(point);
            }

            UpdateIndices();
        }

        private void UpdateIndices()
        {
            for (var i = 0; i < Data.Count; i++)
            {
                Data[i].Index = i + 1;
            }
        }

        private void ExecuteSaveCommand()
        {
            var sfd = new SaveFileDialog
            {
                FileName = "",
                Filter = "Text files|*.txt|CSV files|*.csv|Graph Digitizer Files|*.gdf",
            };

            if (sfd.ShowDialog() != true)
            {
                return;
            }

            switch (sfd.FilterIndex)
            {
                case 1:
                    using (var sw = new System.IO.StreamWriter(sfd.OpenFile()))
                    {
                        sw.WriteLine("{0,-22}{1,-22}", "X Value", "Y Value");
                        sw.WriteLine(new string('-', 45));
                        foreach (var p in this.Data)
                        {
                            sw.WriteLine("{0,-22}{1,-22}", p.Transformed.X, p.Transformed.Y);
                        }

                        sw.Close();
                    }
                    break;
                case 2:
                    using (var sw = new System.IO.StreamWriter(sfd.OpenFile()))
                    {
                        var sep = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                        sw.WriteLine("X Value" + sep + "Y Value");
                        foreach (var p in this.Data)
                        {
                            sw.WriteLine(p.Transformed.X + sep + p.Transformed.Y);
                        }

                        sw.Close();
                    }
                    break;
                case 3:
                    using (var bw = new System.IO.BinaryWriter(sfd.OpenFile()))
                    {
                        var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                        if (!(this.TargetImage?.Source is BitmapImage bitmapImage))
                        {
                            MessageBox.Show(
                                "This file format does not support the type of image you are using.",
                                "Unsupported Image Type",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        var bmp = BufferFromImage(bitmapImage);
                        bw.Write(bmp.Length);
                        bw.Write(bmp);

                        //Proportion and zoom
                        bw.Write(this.CanvasFactor);
                        bw.Write(this.Zoom);

                        //X axis
                        bw.Write(Axes.X.Minimum.X); bw.Write(Axes.X.Minimum.Y); bw.Write(Axes.X.MinimumValue);
                        bw.Write(Axes.X.Maximum.X); bw.Write(Axes.X.Maximum.Y); bw.Write(Axes.X.MaximumValue);
                        bw.Write(Axes.XLog);

                        //Y axis
                        bw.Write(Axes.Y.Minimum.X); bw.Write(Axes.Y.Minimum.Y); bw.Write(Axes.Y.MinimumValue);
                        bw.Write(Axes.Y.Maximum.X); bw.Write(Axes.Y.Maximum.Y); bw.Write(Axes.Y.MaximumValue);
                        bw.Write(Axes.YLog);

                        //Points
                        bw.Write(this.Data.Count);
                        foreach (var p in this.Data)
                        {
                            bw.Write(p.Transformed.X);
                            bw.Write(p.Transformed.Y);
                            bw.Write(p.Relative.X);
                            bw.Write(p.Relative.Y);
                        }

                        bw.Close();
                        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    break;
            }
        }

        public void SelectPoint(RelativePoint relative)
        {
            if (State == State.Axes)
            {
                switch (Axes.Status)
                {
                    case 0:
                        Axes.X.Minimum = relative;
                        break;
                    case 1:
                        Axes.X.Maximum = relative;
                        break;
                    case 2:
                        Axes.Y.Minimum = relative;
                        break;
                    case 3:
                        Axes.Y.Maximum = relative;
                        break;
                }
                Axes.Status++;
                if (Axes.Status == 4)
                    SelectAxesProp();
            }
            else if (State == State.Points)
            {
                AddPoint(relative);
            }

            SetToolTip();
        }

        private void ExecuteAxesCommand()
        {
            State = State.Axes;
            Axes.Status = 0;
            SetToolTip();
        }

        private void ExecuteCopyCommand()
        {
            var res = string.Join(Environment.NewLine, Data.Select(x => $"{x.Transformed.X}\t{x.Transformed.Y}"));
            Clipboard.SetText(res);
        }

        private void ExecuteClearDataCommand()
        {
            Data.Clear();
        }

        private void ExecuteZoomInCommand()
        {
            if (Zoom < 16) Zoom *= 2;
        }

        private void ExecuteZoomOutCommand()
        {
            if (Zoom > 1) Zoom /= 2;
        }

        private void ExecuteEnlargeCommand()
        {
            CanvasFactor = Math.Min(30, CanvasFactor + 1);
        }

        private void ExecuteReduceCommand()
        {
            CanvasFactor = Math.Max(-30, CanvasFactor - 1);
        }

        private void ExecuteRestoreCommand()
        {
            CanvasFactor = 0;
        }
    }
}
