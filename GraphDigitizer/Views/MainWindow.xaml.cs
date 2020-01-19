using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;
using GraphDigitizer.ViewModels.Graphics;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel;

        private bool precisionMode = false;
        
        private Point previousPosition;

        //Selection rectangle
        private bool selecting = false; //Selection rectangle off
        private Point selFirstPos; //First rectangle corner
        private Rectangle selRect; //SelectionRectangle

        //Help window
        private Help helpWindow;

        public MainWindow()
        {
            InitializeComponent();

            viewModel = (MainWindowViewModel) DataContext;

            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                this.viewModel.OpenFile(Properties.Settings.Default.LastFile);
            }
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                FileName = "",
                Filter = "Image files|*.bmp;*.gif;*.tiff;*.jpg;*.jpeg;*.png|Graph Digitizer files|*.gdf",
            };

            if (ofd.ShowDialog() != true)
            {
                return;
            }

            if (ofd.FilterIndex == 1)
            {
                this.viewModel.OpenFile(ofd.FileName);
                Properties.Settings.Default.LastFile = ofd.FileName;
                Properties.Settings.Default.Save();
            }
            else if (ofd.FilterIndex == 2)
            {
                var br = new System.IO.BinaryReader(ofd.OpenFile());
                var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                var bmp = this.ImageFromBuffer(br.ReadBytes(br.ReadInt32()));

                this.viewModel.CanvasFactor = (int)(Math.Log(br.ReadInt32() / 100.0) / Math.Log(1.2));
                this.Zoom = br.ReadInt32();

                this.viewModel.TargetImage = new TargetImage
                {
                    Width = bmp.PixelWidth,
                    Height = bmp.PixelHeight,
                    Source = bmp,
                };

                Axes.X.Minimum.X = br.ReadDouble(); Axes.X.Minimum.Y = br.ReadDouble(); Axes.X.MinimumValue = br.ReadDouble();
                Axes.X.Maximum.X = br.ReadDouble(); Axes.X.Maximum.Y = br.ReadDouble(); Axes.X.MaximumValue = br.ReadDouble();
                Axes.XLog = br.ReadBoolean();

                Axes.Y.Minimum.X = br.ReadDouble(); Axes.Y.Minimum.Y = br.ReadDouble(); Axes.Y.MinimumValue = br.ReadDouble();
                Axes.Y.Maximum.X = br.ReadDouble(); Axes.Y.Maximum.Y = br.ReadDouble(); Axes.Y.MaximumValue = br.ReadDouble();
                Axes.YLog = br.ReadBoolean();

                DeletePoints();
                var total = br.ReadInt32();
                for (var i = 0; i < total; i++)
                {
                    var transformed = new TransformedPoint(br.ReadDouble(), br.ReadDouble());
                    var relative = new RelativePoint(br.ReadDouble(), br.ReadDouble());
                    this.viewModel.Data.Add(new DataPoint(transformed, relative, i + 1));
                }

                viewModel.State = State.Points;
                SetToolTip();
                viewModel.CanvasCursor = Cursors.Cross;

                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {
            var graph = (UIElement)sender;
            var p = e.GetPosition(graph);
            if (selecting) //Update selection rectangle position
            {
                if (p.X > selFirstPos.X)
                    selRect.Width = p.X - selFirstPos.X;
                else
                {
                    Canvas.SetLeft(selRect, p.X);
                    selRect.Width = selFirstPos.X - p.X;
                }

                if (p.Y > selFirstPos.Y)
                    selRect.Height = p.Y - selFirstPos.Y;
                else
                {
                    Canvas.SetTop(selRect, p.Y);
                    selRect.Height = selFirstPos.Y - p.Y;
                }
            }

            viewModel.MousePosition = new RelativePoint(p.X / viewModel.TargetImage.Width, p.Y / viewModel.TargetImage.Height);

            if (viewModel.State == State.Axes)
            {
                if (Axes.Status == 1)
                {
                    Axes.X.Maximum = viewModel.MousePosition;
                }
                else if (Axes.Status == 3)
                {
                    Axes.Y.Maximum = viewModel.MousePosition;
                }
            }
        }

        public static string FormatNum(double num, int exponentialDecimals = 4, int floatDecimals = 8)
        {
            if (double.IsNaN(num)) return "N/A";
            if (Math.Abs(num) < 1e-10) return "0";

            var aux = Math.Abs(num);
            if (aux > Math.Pow(10, exponentialDecimals + 2) - 1 || aux < Math.Pow(10, -exponentialDecimals - 1))
            {
                return num.ToString($"E{exponentialDecimals}");
            }

            var dig = (int)(Math.Log10(aux) + Math.Sign(Math.Log10(aux)));
            return num.ToString(dig >= 0 ? $"F{floatDecimals - dig}" : $"F{floatDecimals}");
        }

        private void cnvZoom_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(imgZoom);
            viewModel.UpdateStatusCoords(new RelativePoint(p.X / imgZoom.ActualWidth * viewModel.TargetImage.Width, p.Y / imgZoom.ActualHeight * viewModel.TargetImage.Height));
        }

        private void ZoomModeIn()
        {
            // TODO: Hard-coded 100 / 200 (should refer to actual canvas size)
            precisionMode = true;
            MouseUtils.GetCursorPos(out var prev);
            previousPosition.X = (double)prev.X;
            previousPosition.Y = (double)prev.Y;
            var p = PointToScreen(cnvZoom.TransformToAncestor(this).Transform(new Point(0, 0)));
            MouseUtils.SetCursorPos((int)p.X + 100, (int)p.Y + 100);

            MouseUtils.Rect r;
            r.Top = (int)p.Y;
            r.Bottom = (int)p.Y + 200;
            r.Left = (int)p.X;
            r.Right = (int)p.X + 200;
            MouseUtils.ClipCursor(ref r);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && !precisionMode && this.viewModel.IsMouseOverCanvas)
            {
                ZoomModeIn();
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || 
                Keyboard.IsKeyDown(Key.RightCtrl) || 
                Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) || 
                Keyboard.IsKeyDown(Key.LeftShift) || 
                Keyboard.IsKeyDown(Key.RightShift))
            {
                return;
            }

            // TODO: This should all be driven from xaml
            switch (e.Key)
            {
                case Key.F1:
                    OnHelpClicked(sender, e);
                    break;
                case Key.D1:
                    viewModel.SelectModeCommand.Execute(null);
                    break;
                case Key.D2:
                    viewModel.PointsModeCommand.Execute(null);
                    break;
            }
        }

        private void ZoomModeOut(bool recover = true)
        {
            MouseUtils.Rect r;
            precisionMode = false;
            r.Top = int.MinValue;
            r.Bottom = int.MaxValue;
            r.Left = int.MinValue;
            r.Right = int.MaxValue;
            MouseUtils.ClipCursor(ref r);
            if (recover) {
                MouseUtils.SetCursorPos((int)previousPosition.X, (int)previousPosition.Y);
            }
        }

        private void OnWindowPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && precisionMode)
                ZoomModeOut();
        }

        private void AddPoint(RelativePoint position)
        {
            var transformed = this.viewModel.GetRealCoords(position);
            var p = new DataPoint(transformed, position, this.viewModel.Data.Count + 1);
            this.viewModel.Data.Add(p);
        }

        //private void CreateXaxis()
        //{
        //    if (Axes.XAxis != null)
        //    {
        //        // TODO
        //        //cnvGraph.Children.Remove(Axes.Xaxis);
        //    }

        //    Axes.XAxis = new Line
        //    {
        //        X1 = Axes.XMin.X,
        //        Y1 = Axes.XMin.Y,
        //        X2 = Axes.XMax.X,
        //        Y2 = Axes.XMax.Y,
        //        Stroke = Brushes.Red,
        //        StrokeThickness = 2,
        //        StrokeDashArray = new DoubleCollection
        //        {
        //            5.0, 5.0
        //        },
        //        StrokeEndLineCap = PenLineCap.Triangle
        //    };

        //    // TODO
        //    //cnvGraph.Children.Add(Axes.Xaxis);
        //}

        //private void CreateYaxis()
        //{
        //    if (Axes.YAxis != null)
        //    {
        //        // TODO
        //        //cnvGraph.Children.Remove(Axes.Yaxis);
        //    }

        //    Axes.YAxis = new Line
        //    {
        //        X1 = Axes.YMin.X,
        //        Y1 = Axes.YMin.Y,
        //        X2 = Axes.YMax.X,
        //        Y2 = Axes.YMax.Y,
        //        Stroke = Brushes.Blue,
        //        StrokeThickness = 2,
        //        StrokeDashArray = new DoubleCollection
        //        {
        //            5.0, 5.0
        //        },
        //        StrokeEndLineCap = PenLineCap.Round
        //    };

        //    // TODO
        //    //cnvGraph.Children.Add(Axes.Yaxis);
        //}

        private void SelectPoint(RelativePoint relative)
        {
            if (viewModel.State == State.Axes)
            {
                switch (Axes.Status)
                {
                    case 0: //Xmin
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
            else if (viewModel.State == State.Points)
            {
                AddPoint(relative);
            }
            SetToolTip();
        }

        private void UpdateData()
        {
            for (var i = 0; i < this.viewModel.Data.Count; i++)
            {
                this.viewModel.Data[i].Index = (i + 1) % 100;
            }

            dgrPoints.Items.Refresh();
        }

        private void PointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (viewModel.State != State.Select) return;
            if (e.ChangedButton == MouseButton.Left) //Select mode
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) //Add to current selection. If it was already selected, unselect
                {
                    if (dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                        dgrPoints.SelectedItems.Remove(((Label)sender).Tag);
                    else
                        dgrPoints.SelectedItems.Add(((Label)sender).Tag);
                }
                else if (dgrPoints.SelectedItems.Count == 1 && dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                    dgrPoints.SelectedItems.Clear();
                else
                {
                    dgrPoints.SelectedItems.Clear();
                    dgrPoints.SelectedItem = ((Label)sender).Tag;
                }
            }
            else if (e.ChangedButton == MouseButton.Right) //Delete mode
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    DeleteSelection(sender, e);
                else
                {
                    this.viewModel.Data.Remove((DataPoint)((Label)sender).Tag);
                    UpdateData();
                }
            }
        }

        private void DeleteSelection(object sender, EventArgs e)
        {
            foreach (DataPoint dp in dgrPoints.SelectedItems)
            {
                this.viewModel.Data.Remove(dp);
            }

            UpdateData();
        }

        private void imgGraph_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this.DataCanvas);
            if (viewModel.State == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                selecting = true;
                selFirstPos = p;
                selRect = new Rectangle() { Stroke = new SolidColorBrush(new Color() { ScA = 0.7f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), Fill = new SolidColorBrush(new Color() { ScA = 0.2f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), StrokeThickness = 1.0 };

                // TODO
                //this.cnvGraph.Children.Add(this.selRect);

                //Canvas.SetLeft(this.selRect, this.selFirstPos.X);
                //Canvas.SetTop(this.selRect, this.selFirstPos.Y);
            }
            else
                this.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(this.DataCanvas.ActualWidth, this.DataCanvas.ActualHeight));
        }

        private void imgZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(imgZoom);
            this.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(imgZoom.ActualWidth, imgZoom.ActualHeight));
        }

        private void btnAxes_Click(object sender, RoutedEventArgs e)
        {
            viewModel.State = State.Axes;
            Axes.Status = 0;
            SetToolTip();
        }

        private void btnAxesProp_Click(object sender, RoutedEventArgs e)
        {
            SelectAxesProp();
        }

        private void SelectAxesProp()
        {
            if (precisionMode) ZoomModeOut(false);
            var ap = new AxesProp(Axes);
            MouseUtils.GetCursorPos(out var p);
            //The program will try to position the window leaving the mouse in a corner
            if (p.X + ap.Width > SystemParameters.PrimaryScreenWidth)
                ap.Left = p.X - ap.Width + 20;
            else
                ap.Left = p.X;

            if (p.Y + ap.Height > SystemParameters.PrimaryScreenHeight - 50) // Threshold for the Windows taskbar
                ap.Top = p.Y - ap.Height;
            else
                ap.Top = p.Y;

            ap.ShowDialog();
            Axes = ap.Axes;
            viewModel.State = State.Points;
        }

        private void DeletePoints()
        {
            this.viewModel.Data.Clear();
        }

        private void OnDeletePointsClicked(object sender, RoutedEventArgs e)
        {
            DeletePoints();
        }

        private void OnZoomInClicked(object sender, RoutedEventArgs e)
        {
            if (this.Zoom < 16) this.Zoom *= 2;
        }

        private void OnZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (this.Zoom > 1) this.Zoom /= 2;
        }

        private void OnEnlargeClicked(object sender, RoutedEventArgs e)
        {
            this.viewModel.CanvasFactor = Math.Min(30, this.viewModel.CanvasFactor + 1);
        }

        private void OnReduceClicked(object sender, RoutedEventArgs e)
        {
            this.viewModel.CanvasFactor = Math.Max(-30, this.viewModel.CanvasFactor - 1);
        }

        private void OnRestoreClicked(object sender, RoutedEventArgs e)
        {
            this.viewModel.CanvasFactor = 0;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Zoom = this.Zoom;
            Properties.Settings.Default.Proportion = this.viewModel.CanvasFactor;
            Properties.Settings.Default.Save();
        }

        private void OnCopyClicked(object sender, RoutedEventArgs e)
        {
            var res = string.Join(Environment.NewLine, this.viewModel.Data.Select(x => $"{x.Transformed.X}\t{x.Transformed.Y}"));
            Clipboard.SetText(res);
        }

        private void OnWindowPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!selecting)
            {
                return;
            }

            if (double.IsNaN(selRect.Width) || selRect.Width < 1.0 || double.IsNaN(selRect.Height) || selRect.Height < 1.0)
            {
                //Nothing at the moment
            }
            else
            {
                double left = Canvas.GetLeft(selRect), top = Canvas.GetTop(selRect);
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    dgrPoints.SelectedItems.Clear();
                // TODO
                //for (int i = 1; i < this.cnvGraph.Children.Count; i++) //Index = 1 is always the imgGraph element
                //{
                //    if (this.cnvGraph.Children[i] is Label)
                //    {
                //        tb = (Label)this.cnvGraph.Children[i];
                //        x = Canvas.GetLeft(tb) + 8;
                //        y = Canvas.GetTop(tb) + 8;
                //        if (x >= left && x <= left + this.selRect.Width && y >= top && y <= top + this.selRect.Height) //Point is within the rectangle
                //            this.dgrPoints.SelectedItems.Add((DataPoint)tb.Tag);
                //    }
                //}
            }

            // TODO
            //this.cnvGraph.Children.Remove(this.selRect);
            selecting = false;
        }

        private void OnHelpClicked(object sender, RoutedEventArgs e)
        {
            if (helpWindow == null || !helpWindow.IsLoaded)
            {
                helpWindow = new Help();
                helpWindow.Show();
            }
            else
            {
                helpWindow.Focus();
                if (helpWindow.WindowState == WindowState.Minimized)
                    helpWindow.WindowState = WindowState.Normal;
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
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
                        foreach (var p in this.viewModel.Data)
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
                        foreach (var p in this.viewModel.Data)
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

                        if (!(this.viewModel.TargetImage?.Source is BitmapImage bitmapImage))
                        {
                            MessageBox.Show(
                                "This file format does not support the type of image you are using.",
                                "Unsupported Image Type",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        var bmp = this.BufferFromImage(bitmapImage);
                        bw.Write(bmp.Length);
                        bw.Write(bmp);

                        //Proportion and zoom
                        bw.Write(this.viewModel.CanvasFactor);
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
                        bw.Write(this.viewModel.Data.Count);
                        foreach (var p in this.viewModel.Data)
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

        public BitmapImage ImageFromBuffer(byte[] bytes)
        {
            var stream = new System.IO.MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }

        public byte[] BufferFromImage(BitmapImage imageSource)
        {
            var ms = new System.IO.MemoryStream();
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(imageSource));
            enc.Save(ms);
            return ms.ToArray();
        }

        // TODO: All code below should be removed once MVVM is fully implemented

        private int Zoom
        {
            get => viewModel.Zoom;
            set => viewModel.Zoom = value;
        }

        private Axes Axes
        {
            get => viewModel.Axes;
            set => viewModel.Axes = value;
        }

        private void SetToolTip()
        {
            // TODO: Make SetToolTip() private after removing this
            viewModel.SetToolTip();
        }

        private void OnTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Mark those points as selected
        }

        private void MouseEnterEventHandler(object sender, MouseEventArgs e)
        {
            this.viewModel.IsMouseOverCanvas = true;
        }

        private void MouseLeaveEventHandler(object sender, MouseEventArgs e)
        {
            this.viewModel.IsMouseOverCanvas = false;
        }
    }
}
