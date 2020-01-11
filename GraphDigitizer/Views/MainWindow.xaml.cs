using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;
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

        private readonly List<DataPoint> data = new List<DataPoint>();

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

            dgrPoints.ItemsSource = data;
            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                OpenFile(Properties.Settings.Default.LastFile);
            }
        }

        private void OpenFile(string path)
        {
            var bmp = new BitmapImage(new Uri(path));

            //Since everything will be deleted, there is no need for calling UpdateProportions(100.0)
            this.viewModel.CanvasFactor = 0;

            this.viewModel.TargetImage = new TargetImage
            {
                Source = bmp,
                Width = bmp.PixelWidth,
                Height = bmp.PixelHeight,
            };

            this.imgZoom.Width = bmp.PixelWidth * this.Zoom;
            this.imgZoom.Height = bmp.PixelHeight * this.Zoom;
            this.imgZoom.Source = bmp;

            viewModel.State = State.Axes;
            Axes.Status = 0;

            this.DeletePoints();
            if (this.Axes.Xaxis != null)
            {
                // TODO
                //this.cnvGraph.Children.Remove(this.Axes.Xaxis);
            }

            if (this.Axes.Yaxis != null)
            {
                // TODO
                //this.cnvGraph.Children.Remove(this.Axes.Yaxis);
            }

            Axes.Xmin.X = Axes.Xmin.Y = Axes.Xmax.X = Axes.Xmax.Y = Axes.Ymin.X = Axes.Ymin.Y = Axes.Ymax.X = Axes.Ymax.Y = double.NaN;

            SetToolTip();
            viewModel.CanvasCursor = Cursors.Cross;
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
                this.OpenFile(ofd.FileName);
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

                this.imgZoom.Width = bmp.PixelWidth * this.Zoom;
                this.imgZoom.Height = bmp.PixelHeight * this.Zoom;
                this.imgZoom.Source = this.viewModel.TargetImage.Source;

                Axes.Xmin.X = br.ReadDouble(); Axes.Xmin.Y = br.ReadDouble(); Axes.Xmin.Value = br.ReadDouble();
                Axes.Xmax.X = br.ReadDouble(); Axes.Xmax.Y = br.ReadDouble(); Axes.Xmax.Value = br.ReadDouble();
                Axes.XLog = br.ReadBoolean();
                CreateXaxis();

                Axes.Ymin.X = br.ReadDouble(); Axes.Ymin.Y = br.ReadDouble(); Axes.Ymin.Value = br.ReadDouble();
                Axes.Ymax.X = br.ReadDouble(); Axes.Ymax.Y = br.ReadDouble(); Axes.Ymax.Value = br.ReadDouble();
                Axes.YLog = br.ReadBoolean();
                CreateYaxis();

                DeletePoints();
                var total = br.ReadInt32();
                data.Capacity = total;
                for (var i = 0; i < total; i++)
                {
                    this.data.Add(new DataPoint(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), i + 1));
                }

                dgrPoints.Items.Refresh();

                viewModel.State = State.Points;
                SetToolTip();
                viewModel.CanvasCursor = Cursors.Cross;

                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(sender as IInputElement);
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

            viewModel.UpdateStatusCoords(p.X, p.Y);
            Canvas.SetLeft(this.imgZoom, 100 - p.X * this.Zoom);
            Canvas.SetTop(this.imgZoom, 100 - p.Y * this.Zoom);

            if (viewModel.State == State.Axes)
            {
                if (Axes.Status == 1)
                {
                    Axes.Xaxis.X2 = p.X;
                    Axes.Xaxis.Y2 = p.Y;
                }
                else if (Axes.Status == 3)
                {
                    Axes.Yaxis.X2 = p.X;
                    Axes.Yaxis.Y2 = p.Y;
                }
            }
        }

        public static string FormatNum(double num)
        {
            if (double.IsNaN(num)) return "N/A";
            if (Math.Abs(num) < 1e-10) return "0";

            var aux = Math.Abs(num);
            if (aux > 999999.0 || aux < 0.00001)
            {
                return num.ToString("E4");
            }

            var dig = (int)(Math.Log10(aux) + Math.Sign(Math.Log10(aux)));
            return num.ToString(dig >= 0 ? $"F{8 - dig}" : "F8");
        }

        private void cnvZoom_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(imgZoom);
            viewModel.UpdateStatusCoords(p.X / Zoom, p.Y / Zoom);
        }

        private void ZoomModeIn()
        {
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
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) /* && cnvGraph.IsMouseOver TODO*/)
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

        private void AddPoint(double x, double y)
        {
            var (xPoint, yPoint) = this.viewModel.GetRealCoords(x, y);
            var p = new DataPoint(xPoint, yPoint, x, y, this.data.Count + 1);
            this.data.Add(p);
            this.viewModel.Data.Add(p);
            dgrPoints.Items.Refresh();
        }

        private void CreateXaxis()
        {
            if (Axes.Xaxis != null)
            {
                // TODO
                //cnvGraph.Children.Remove(Axes.Xaxis);
            }

            Axes.Xaxis = new Line
            {
                X1 = Axes.Xmin.X,
                Y1 = Axes.Xmin.Y,
                X2 = Axes.Xmax.X,
                Y2 = Axes.Xmax.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Triangle
            };

            // TODO
            //cnvGraph.Children.Add(Axes.Xaxis);
        }

        private void CreateYaxis()
        {
            if (Axes.Yaxis != null)
            {
                // TODO
                //cnvGraph.Children.Remove(Axes.Yaxis);
            }

            Axes.Yaxis = new Line
            {
                X1 = Axes.Ymin.X,
                Y1 = Axes.Ymin.Y,
                X2 = Axes.Ymax.X,
                Y2 = Axes.Ymax.Y,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Round
            };

            // TODO
            //cnvGraph.Children.Add(Axes.Yaxis);
        }

        private void SelectPoint(double X, double Y)
        {
            if (viewModel.State == State.Axes)
            {
                switch (Axes.Status)
                {
                    case 0: //Xmin
                        Axes.Xmin.X = X;
                        Axes.Xmin.Y = Y;
                        Axes.Xmax.X = X;
                        Axes.Xmax.Y = Y;
                        CreateXaxis();
                        break;
                    case 1:
                        Axes.Xmax.X = X;
                        Axes.Xmax.Y = Y;
                        CreateXaxis();
                        break;
                    case 2:
                        Axes.Ymin.X = X;
                        Axes.Ymin.Y = Y;
                        Axes.Ymax.X = X;
                        Axes.Ymax.Y = Y;
                        CreateYaxis();
                        break;
                    case 3:
                        Axes.Ymax.X = X;
                        Axes.Ymax.Y = Y;
                        CreateYaxis();
                        break;
                }
                Axes.Status++;
                if (Axes.Status == 4)
                    SelectAxesProp();
            }
            else if (viewModel.State == State.Points)
            {
                AddPoint(X, Y);
            }
            SetToolTip();
        }

        private void UpdateData()
        {
            for (var i = 0; i < this.viewModel.Data.Count; i++)
            {
                data[i].Index = (i + 1) % 100;
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
                    data.Remove((DataPoint)((Label)sender).Tag);
                    UpdateData();
                }
            }
        }

        private void DeleteSelection(object sender, EventArgs e)
        {
            foreach (DataPoint dp in dgrPoints.SelectedItems)
            {
                data.Remove(dp);
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
                this.SelectPoint(p.X, p.Y);
        }

        private void imgZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(imgZoom);
            SelectPoint(p.X / Zoom, p.Y / Zoom);
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
            imgZoom.Width = ((BitmapSource)imgZoom.Source).PixelWidth * this.Zoom;
            imgZoom.Height = ((BitmapSource)imgZoom.Source).PixelHeight * this.Zoom;
        }

        private void OnZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (this.Zoom > 1) this.Zoom /= 2;
            imgZoom.Width = ((BitmapSource)imgZoom.Source).PixelWidth * this.Zoom;
            imgZoom.Height = ((BitmapSource)imgZoom.Source).PixelHeight * this.Zoom;
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
            var res = string.Empty;
            for (var i = 0; i < data.Count; i++)
            {
                res += data[i].X + "\t" + data[i].Y;
                if (i != data.Count) res += Environment.NewLine;
            }

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
                        foreach (var p in this.data)
                        {
                            sw.WriteLine("{0,-22}{1,-22}", p.X, p.Y);
                        }

                        sw.Close();
                    }
                    break;
                case 2:
                    using (var sw = new System.IO.StreamWriter(sfd.OpenFile()))
                    {
                        var sep = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                        sw.WriteLine("X Value" + sep + "Y Value");
                        foreach (var p in this.data)
                        {
                            sw.WriteLine(p.X + sep + p.Y);
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
                        bw.Write(Axes.Xmin.X); bw.Write(Axes.Xmin.Y); bw.Write(Axes.Xmin.Value);
                        bw.Write(Axes.Xmax.X); bw.Write(Axes.Xmax.Y); bw.Write(Axes.Xmax.Value);
                        bw.Write(Axes.XLog);

                        //Y axis
                        bw.Write(Axes.Ymin.X); bw.Write(Axes.Ymin.Y); bw.Write(Axes.Ymin.Value);
                        bw.Write(Axes.Ymax.X); bw.Write(Axes.Ymax.Y); bw.Write(Axes.Ymax.Value);
                        bw.Write(Axes.YLog);

                        //Points
                        bw.Write(data.Count);
                        foreach (var p in data)
                        {
                            bw.Write(p.X);
                            bw.Write(p.Y);
                            bw.Write(p.RealX);
                            bw.Write(p.RealY);
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

        private void btnFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("The Clipboard does not contain a valid image.", "Invalid Clipboard content", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bmp = Clipboard.GetImage();

            //Since everything will be deleted, there is no need for calling UpdateProportions(100.0)
            this.viewModel.CanvasFactor = 0;

            this.viewModel.TargetImage = new TargetImage
            {
                Source = bmp,
                Width = bmp.PixelWidth,
                Height = bmp.PixelHeight
            };

            this.imgZoom.Width = bmp.PixelWidth * this.Zoom;
            this.imgZoom.Height = bmp.PixelHeight * this.Zoom;
            this.imgZoom.Source = bmp;

            this.viewModel.State = State.Axes;
            this.Axes.Status = 0;

            this.DeletePoints();
            if (this.Axes.Xaxis != null)
            {
                // TODO
                //this.cnvGraph.Children.Remove(this.Axes.Xaxis);
            }

            if (this.Axes.Yaxis != null)
            {
                // TODO
                //this.cnvGraph.Children.Remove(this.Axes.Yaxis);
            }

            Axes.Xmin.X = Axes.Xmin.Y = Axes.Xmax.X = Axes.Xmax.Y = Axes.Ymin.X = Axes.Ymin.Y = Axes.Ymax.X = Axes.Ymax.Y = double.NaN;

            SetToolTip();
            viewModel.CanvasCursor = Cursors.Cross;
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
    }
}
