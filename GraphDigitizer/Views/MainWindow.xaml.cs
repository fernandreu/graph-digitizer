using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        private System.Windows.Point previousPosition;

        private readonly List<DataPoint> data = new List<DataPoint>();

        //Zoom properties, prop in percentage
        private int zoom = 2, prop = 100; 
        
        //Selection rectangle
        private bool selecting = false; //Selection rectangle off
        private System.Windows.Point selFirstPos; //First rectangle corner
        private Rectangle selRect; //SelectionRectangle

        //Help window
        private Help helpWindow;

        private readonly OpenFileDialog ofd = new OpenFileDialog
        {
            FileName = "", 
            Filter = "Image files|*.bmp;*.gif;*.tiff;*.jpg;*.jpeg;*.png|Graph Digitizer files|*.gdf",
        };

        private readonly SaveFileDialog sfd = new SaveFileDialog
        {
            FileName = "", 
            Filter = "Text files|*.txt|CSV files|*.csv|Graph Digitizer Files|*.gdf",
        };

        public MainWindow()
        {
            this.InitializeComponent();

            this.viewModel = (MainWindowViewModel) this.DataContext;

            this.dgrPoints.ItemsSource = this.data;
            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                this.OpenFile(Properties.Settings.Default.LastFile);
            }

            this.UpdateProportions((double)Properties.Settings.Default.Proportion);
        }

        private void OpenFile(string path)
        {
            var bmp = new BitmapImage(new Uri(path));

            //Since everything will be deleted, there is no need for calling UpdateProportions(100.0)
            this.prop = 100;

            this.imgGraph.Width = bmp.PixelWidth;
            this.imgGraph.Height = bmp.PixelHeight;
            this.imgGraph.Source = bmp;
            this.cnvGraph.Width = bmp.PixelWidth;
            this.cnvGraph.Height = bmp.PixelHeight;

            this.imgZoom.Width = bmp.PixelWidth * this.zoom;
            this.imgZoom.Height = bmp.PixelHeight * this.zoom;
            this.imgZoom.Source = bmp;

            this.viewModel.State = State.Axes;
            this.Axes.Status = 0;

            this.DeletePoints();
            if (this.Axes.Xaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Xaxis);
            }

            if (this.Axes.Yaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Yaxis);
            }

            this.Axes.Xmin.X = this.Axes.Xmin.Y = this.Axes.Xmax.X = this.Axes.Xmax.Y = this.Axes.Ymin.X = this.Axes.Ymin.Y = this.Axes.Ymax.X = this.Axes.Ymax.Y = double.NaN;

            this.SetToolTip();
            this.viewModel.CanvasCursor = Cursors.Cross;
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e)
        {
            if (!this.ofd.ShowDialog().Value)
            {
                return;
            }

            if (this.ofd.FilterIndex == 1)
            {
                this.OpenFile(this.ofd.FileName);
                Properties.Settings.Default.LastFile = this.ofd.FileName;
                Properties.Settings.Default.Save();
            }
            else if (this.ofd.FilterIndex == 2)
            {
                var br = new System.IO.BinaryReader(this.ofd.OpenFile());
                var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                var bmp = this.ImageFromBuffer(br.ReadBytes(br.ReadInt32()));

                this.prop = br.ReadInt32();
                this.zoom = br.ReadInt32();

                this.imgGraph.Width = bmp.PixelWidth * this.prop * 0.01;
                this.imgGraph.Height = bmp.PixelHeight * this.prop * 0.01;
                this.imgGraph.Source = bmp;
                this.cnvGraph.Width = bmp.PixelWidth * this.prop * 0.01;
                this.cnvGraph.Height = bmp.PixelHeight * this.prop * 0.01;

                this.imgZoom.Width = bmp.PixelWidth * this.zoom;
                this.imgZoom.Height = bmp.PixelHeight * this.zoom;
                this.imgZoom.Source = bmp;

                this.Axes.Xmin.X = br.ReadDouble(); this.Axes.Xmin.Y = br.ReadDouble(); this.Axes.Xmin.Value = br.ReadDouble();
                this.Axes.Xmax.X = br.ReadDouble(); this.Axes.Xmax.Y = br.ReadDouble(); this.Axes.Xmax.Value = br.ReadDouble();
                this.Axes.XLog = br.ReadBoolean();
                this.CreateXaxis();

                this.Axes.Ymin.X = br.ReadDouble(); this.Axes.Ymin.Y = br.ReadDouble(); this.Axes.Ymin.Value = br.ReadDouble();
                this.Axes.Ymax.X = br.ReadDouble(); this.Axes.Ymax.Y = br.ReadDouble(); this.Axes.Ymax.Value = br.ReadDouble();
                this.Axes.YLog = br.ReadBoolean();
                this.CreateYaxis();

                this.DeletePoints();
                var total = br.ReadInt32();
                this.data.Capacity = total;
                for (var i = 0; i < total; i++)
                {
                    this.data.Add(new DataPoint(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), this.cnvGraph, this.prop, i + 1));
                }

                this.dgrPoints.Items.Refresh();

                this.viewModel.State = State.Points;
                this.SetToolTip();
                this.viewModel.CanvasCursor = Cursors.Cross;

                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {

            System.Windows.Point p = e.GetPosition(this.imgGraph);
            if (this.selecting) //Update selection rectangle position
            {
                if (p.X > this.selFirstPos.X)
                    this.selRect.Width = p.X - this.selFirstPos.X;
                else
                {
                    Canvas.SetLeft(this.selRect, p.X);
                    this.selRect.Width = this.selFirstPos.X - p.X;
                }

                if (p.Y > this.selFirstPos.Y)
                    this.selRect.Height = p.Y - this.selFirstPos.Y;
                else
                {
                    Canvas.SetTop(this.selRect, p.Y);
                    this.selRect.Height = this.selFirstPos.Y - p.Y;
                }
            }
            p.X *= 100.0 / this.prop;
            p.Y *= 100.0 / this.prop;
            this.UpdateStatusCoords(p.X, p.Y);
            Canvas.SetLeft(this.imgZoom, 100 - p.X * this.zoom);
            Canvas.SetTop(this.imgZoom, 100 - p.Y * this.zoom);

            if (this.viewModel.State == State.Axes)
            {
                if (this.Axes.Status == 1)
                {
                    this.Axes.Xaxis.X2 = p.X;
                    this.Axes.Xaxis.Y2 = p.Y;
                }
                else if (this.Axes.Status == 3)
                {
                    this.Axes.Yaxis.X2 = p.X;
                    this.Axes.Yaxis.Y2 = p.Y;
                }
            }
        }

        private void UpdateStatusCoords(double X, double Y)
        {
            this.GetRealCoords(X, Y, out var xreal, out var yreal);
            this.txtScreenX.Text = (X * 100 / this.prop).ToString("F2");
            this.txtScreenY.Text = (Y * 100 / this.prop).ToString("F2");
            this.txtRealX.Text = FormatNum(xreal);
            this.txtRealY.Text = FormatNum(yreal);
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
            var p = e.GetPosition(this.imgZoom);
            this.UpdateStatusCoords(p.X / this.zoom, p.Y / this.zoom);
        }

        private void ZoomModeIn()
        {
            this.precisionMode = true;
            MouseUtils.GetCursorPos(out var prev);
            this.previousPosition.X = (double)prev.X;
            this.previousPosition.Y = (double)prev.Y;
            var p = this.PointToScreen(this.cnvZoom.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0)));
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
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && this.cnvGraph.IsMouseOver)
            {
                this.ZoomModeIn();
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
                    this.OnHelpClicked(sender, e);
                    break;
                case Key.D1:
                    this.viewModel.SelectModeCommand.Execute(null);
                    break;
                case Key.D2:
                    this.viewModel.PointsModeCommand.Execute(null);
                    break;
            }
        }

        private void ZoomModeOut(bool recover = true)
        {
            MouseUtils.Rect r;
            this.precisionMode = false;
            r.Top = int.MinValue;
            r.Bottom = int.MaxValue;
            r.Left = int.MinValue;
            r.Right = int.MaxValue;
            MouseUtils.ClipCursor(ref r);
            if (recover) {
                MouseUtils.SetCursorPos((int)this.previousPosition.X, (int)this.previousPosition.Y);
            }
        }

        private void OnWindowPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && this.precisionMode)
                this.ZoomModeOut();
        }

        public void GetRealCoords(double X, double Y, out double RealX, out double RealY)
        {
            double Xaxis, Yaxis;
            //First: obtain the equivalent point in the X axis and in the Y axis
            Xaxis = -((this.Axes.Ymax.X - this.Axes.Ymin.X) * (this.Axes.Xmax.X * this.Axes.Xmin.Y - this.Axes.Xmax.Y * this.Axes.Xmin.X) - (this.Axes.Xmax.X - this.Axes.Xmin.X) * (X * (this.Axes.Ymin.Y - this.Axes.Ymax.Y) + Y * (this.Axes.Ymax.X - this.Axes.Ymin.X))) / ((this.Axes.Xmax.Y - this.Axes.Xmin.Y) * (this.Axes.Ymax.X - this.Axes.Ymin.X) - (this.Axes.Xmax.X - this.Axes.Xmin.X) * (this.Axes.Ymax.Y - this.Axes.Ymin.Y));
            Yaxis = (Y * (this.Axes.Xmax.X - this.Axes.Xmin.X) * (this.Axes.Ymax.Y - this.Axes.Ymin.Y) + (this.Axes.Xmax.Y - this.Axes.Xmin.Y) * (this.Axes.Ymax.Y * this.Axes.Ymin.X - this.Axes.Ymax.X * this.Axes.Ymin.Y + X * (this.Axes.Ymin.Y - this.Axes.Ymax.Y))) / ((this.Axes.Xmin.Y - this.Axes.Xmax.Y) * (this.Axes.Ymax.X - this.Axes.Ymin.X) + (this.Axes.Xmax.X - this.Axes.Xmin.X) * (this.Axes.Ymax.Y - this.Axes.Ymin.Y));

            if (this.Axes.XLog)
                RealX = Math.Pow(10.0, Math.Log10(this.Axes.Xmin.Value) + (Xaxis - this.Axes.Xmin.X) / (this.Axes.Xmax.X - this.Axes.Xmin.X) * (Math.Log10(this.Axes.Xmax.Value) - Math.Log10(this.Axes.Xmin.Value)));
            else
                RealX = this.Axes.Xmin.Value + (Xaxis - this.Axes.Xmin.X) / (this.Axes.Xmax.X - this.Axes.Xmin.X) * (this.Axes.Xmax.Value - this.Axes.Xmin.Value);

            if (this.Axes.YLog)
                RealY = Math.Pow(10.0, Math.Log10(this.Axes.Ymin.Value) + (Yaxis - this.Axes.Ymin.Y) / (this.Axes.Ymax.Y - this.Axes.Ymin.Y) * (Math.Log10(this.Axes.Ymax.Value) - Math.Log10(this.Axes.Ymin.Value)));
            else
                RealY = this.Axes.Ymin.Value + (Yaxis - this.Axes.Ymin.Y) / (this.Axes.Ymax.Y - this.Axes.Ymin.Y) * (this.Axes.Ymax.Value - this.Axes.Ymin.Value);
        }

        private void AddPoint(double x, double y)
        {
            this.GetRealCoords(x, y, out var Xpoint, out var Ypoint);
            this.data.Add(new DataPoint(Xpoint, Ypoint, x, y, this.cnvGraph, this.prop, this.data.Count + 1));
            this.data[this.data.Count - 1].Obj.MouseDown += new MouseButtonEventHandler(this.PointMouseDown);
            this.dgrPoints.Items.Refresh();
        }

        private void CreateXaxis()
        {
            if (this.Axes.Xaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Xaxis);
            }

            this.Axes.Xaxis = new Line
            {
                X1 = this.Axes.Xmin.X * this.prop / 100,
                Y1 = this.Axes.Xmin.Y * this.prop / 100,
                X2 = this.Axes.Xmax.X * this.prop / 100,
                Y2 = this.Axes.Xmax.Y * this.prop / 100,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Triangle
            };

            this.cnvGraph.Children.Add(this.Axes.Xaxis);
        }

        private void CreateYaxis()
        {
            if (this.Axes.Yaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Yaxis);
            }

            this.Axes.Yaxis = new Line
            {
                X1 = this.Axes.Ymin.X * this.prop / 100,
                Y1 = this.Axes.Ymin.Y * this.prop / 100,
                X2 = this.Axes.Ymax.X * this.prop / 100,
                Y2 = this.Axes.Ymax.Y * this.prop / 100,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Round
            };
            this.cnvGraph.Children.Add(this.Axes.Yaxis);
        }

        private void SelectPoint(double X, double Y)
        {
            if (this.viewModel.State == State.Axes)
            {
                switch (this.Axes.Status)
                {
                    case 0: //Xmin
                        this.Axes.Xmin.X = X;
                        this.Axes.Xmin.Y = Y;
                        this.Axes.Xmax.X = X;
                        this.Axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 1:
                        this.Axes.Xmax.X = X;
                        this.Axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 2:
                        this.Axes.Ymin.X = X;
                        this.Axes.Ymin.Y = Y;
                        this.Axes.Ymax.X = X;
                        this.Axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                    case 3:
                        this.Axes.Ymax.X = X;
                        this.Axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                }
                this.Axes.Status++;
                if (this.Axes.Status == 4)
                    this.SelectAxesProp();
            }
            else if (this.viewModel.State == State.Points)
            {
                this.AddPoint(X, Y);
            }
            this.SetToolTip();
        }

        private void UpdateData()
        {
            for (var i = 0; i < this.data.Count; i++)
            {
                this.data[i].Obj.Content = (i + 1) % 100;
            }

            this.dgrPoints.Items.Refresh();
        }

        private void PointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.viewModel.State != State.Select) return;
            if (e.ChangedButton == MouseButton.Left) //Select mode
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) //Add to current selection. If it was already selected, unselect
                {
                    if (this.dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                        this.dgrPoints.SelectedItems.Remove(((Label)sender).Tag);
                    else
                        this.dgrPoints.SelectedItems.Add(((Label)sender).Tag);
                }
                else if (this.dgrPoints.SelectedItems.Count == 1 && this.dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                    this.dgrPoints.SelectedItems.Clear();
                else
                {
                    this.dgrPoints.SelectedItems.Clear();
                    this.dgrPoints.SelectedItem = ((Label)sender).Tag;
                }
            }
            else if (e.ChangedButton == MouseButton.Right) //Delete mode
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    this.DeleteSelection(sender, e);
                else
                {
                    this.cnvGraph.Children.Remove((UIElement)sender);
                    this.data.Remove((DataPoint)((Label)sender).Tag);
                    this.UpdateData();
                }
            }
        }

        private void DeleteSelection(object sender, EventArgs e)
        {
            foreach (DataPoint dp in this.dgrPoints.SelectedItems)
            {
                this.cnvGraph.Children.Remove(dp.Obj);
                this.data.Remove(dp);
            }

            this.UpdateData();
        }

        private void imgGraph_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(this.imgGraph);
            if (this.viewModel.State == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                this.selecting = true;
                this.selFirstPos = p;
                this.selRect = new Rectangle() { Stroke = new SolidColorBrush(new Color() { ScA = 0.7f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), Fill = new SolidColorBrush(new Color() { ScA = 0.2f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), StrokeThickness = 1.0 };
                this.cnvGraph.Children.Add(this.selRect);
                Canvas.SetLeft(this.selRect, this.selFirstPos.X);
                Canvas.SetTop(this.selRect, this.selFirstPos.Y);
            }
            else
                this.SelectPoint(p.X / this.prop * 100, p.Y / this.prop * 100);
        }

        private void imgZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(this.imgZoom);
            this.SelectPoint(p.X / this.zoom, p.Y / this.zoom);
        }

        private void btnAxes_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.State = State.Axes;
            this.Axes.Status = 0;
            this.SetToolTip();
        }

        private void btnAxesProp_Click(object sender, RoutedEventArgs e)
        {
            this.SelectAxesProp();
        }

        private void SelectAxesProp()
        {
            if (this.precisionMode) this.ZoomModeOut(false);
            var ap = new AxesProp(this.Axes);
            MouseUtils.GetCursorPos(out var p);
            //The program will try to position the window leaving the mouse in a corner
            if (p.X + ap.Width > SystemParameters.PrimaryScreenWidth)
                ap.Left = p.X - ap.Width + 20;
            else
                ap.Left = p.X;

            if (p.Y + ap.Height > SystemParameters.PrimaryScreenHeight - 50) // Thresold for the Windows taskbar
                ap.Top = p.Y - ap.Height;
            else
                ap.Top = p.Y;

            ap.ShowDialog();
            this.Axes = ap.Axes;
            this.viewModel.State = State.Points;
        }

        private void DeletePoints()
        {
            foreach (var dp in this.data)
            {
                this.cnvGraph.Children.Remove(dp.Obj);
            }
            
            this.data.Clear();
            this.dgrPoints.Items.Refresh();
        }

        private void OnDeletePointsClicked(object sender, RoutedEventArgs e)
        {
            this.DeletePoints();
        }

        private void OnZoomInClicked(object sender, RoutedEventArgs e)
        {
            if (this.zoom < 16) this.zoom *= 2;
            this.imgZoom.Width = ((BitmapSource)this.imgZoom.Source).PixelWidth * this.zoom;
            this.imgZoom.Height = ((BitmapSource)this.imgZoom.Source).PixelHeight * this.zoom;
        }

        private void OnZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (this.zoom > 1) this.zoom /= 2;
            this.imgZoom.Width = ((BitmapSource)this.imgZoom.Source).PixelWidth * this.zoom;
            this.imgZoom.Height = ((BitmapSource)this.imgZoom.Source).PixelHeight * this.zoom;
        }

        private void UpdateProportions(double newprop)
        {
            this.cnvGraph.Width *= newprop / this.prop;
            this.cnvGraph.Height *= newprop / this.prop;
            this.imgGraph.Width = this.cnvGraph.Width;
            this.imgGraph.Height = this.cnvGraph.Height;

            Line line; Label tb; //tb because originally it was a TextBlock
            for (var i = 1; i < this.cnvGraph.Children.Count; i++) //0 Index always for the imgGraph element
            {
                if (this.cnvGraph.Children[i] is Line)
                {
                    line = (Line)this.cnvGraph.Children[i];
                    line.X1 *= newprop / this.prop;
                    line.Y1 *= newprop / this.prop;
                    line.X2 *= newprop / this.prop;
                    line.Y2 *= newprop / this.prop;
                }
                else if (this.cnvGraph.Children[i] is Label)
                {
                    tb = (Label)this.cnvGraph.Children[i];
                    Canvas.SetLeft(tb, Canvas.GetLeft(tb) * newprop / this.prop);
                    Canvas.SetTop(tb, Canvas.GetTop(tb) * newprop / this.prop);
                }
            }

            this.prop = (int)newprop;
        }

        private void OnEnlargeClicked(object sender, RoutedEventArgs e)
        {
            if (this.prop >= 16000) return;
            double newprop = Math.Floor(this.prop * 1.2);
            if (Math.Abs(newprop - 100.0) < 1.0) newprop = 100.0;
            this.UpdateProportions(newprop);
        }

        private void OnReduceClicked(object sender, RoutedEventArgs e)
        {
            if (this.prop <= 5) return;
            double newprop = Math.Floor(this.prop / 1.2);
            if (Math.Abs(newprop - 100.0) < 1.0) newprop = 100.0;
            this.UpdateProportions(newprop);
            this.prop = (int)newprop;
        }

        private void OnRestoreClicked(object sender, RoutedEventArgs e)
        {
            this.UpdateProportions(100.0);
            this.prop = 100;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Zoom = this.zoom;
            Properties.Settings.Default.Proportion = this.prop;
            Properties.Settings.Default.Save();
        }

        private void OnCopyClicked(object sender, RoutedEventArgs e)
        {
            var res = string.Empty;
            for (var i = 0; i < this.data.Count; i++)
            {
                res += this.data[i].X + "\t" + this.data[i].Y;
                if (i != this.data.Count) res += Environment.NewLine;
            }

            Clipboard.SetText(res);
        }

        private void dgrPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (DataPoint dp in e.RemovedItems)
                dp.Obj.Style = (Style)Application.Current.FindResource("PointStyle");
            foreach (DataPoint dp in e.AddedItems)
                dp.Obj.Style = (Style)Application.Current.FindResource("PointStyleSel");
        }

        private void OnWindowPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.selecting)
            {
                return;
            }

            if (double.IsNaN(this.selRect.Width) || this.selRect.Width < 1.0 || double.IsNaN(this.selRect.Height) || this.selRect.Height < 1.0)
            {
                //Nothing at the moment
            }
            else
            {
                double left = Canvas.GetLeft(this.selRect), top = Canvas.GetTop(this.selRect), x, y;
                Label tb;
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    this.dgrPoints.SelectedItems.Clear();
                for (int i = 1; i < this.cnvGraph.Children.Count; i++) //Index = 1 is always the imgGraph element
                {
                    if (this.cnvGraph.Children[i] is Label)
                    {
                        tb = (Label)this.cnvGraph.Children[i];
                        x = Canvas.GetLeft(tb) + 8;
                        y = Canvas.GetTop(tb) + 8;
                        if (x >= left && x <= left + this.selRect.Width && y >= top && y <= top + this.selRect.Height) //Point is within the rectangle
                            this.dgrPoints.SelectedItems.Add((DataPoint)tb.Tag);
                    }
                }
            }
            this.cnvGraph.Children.Remove(this.selRect);
            this.selecting = false;
        }

        private void OnHelpClicked(object sender, RoutedEventArgs e)
        {
            if (this.helpWindow == null || !this.helpWindow.IsLoaded)
            {
                this.helpWindow = new Help();
                this.helpWindow.Show();
            }
            else
            {
                this.helpWindow.Focus();
                if (this.helpWindow.WindowState == WindowState.Minimized)
                    this.helpWindow.WindowState = WindowState.Normal;
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (!this.sfd.ShowDialog().Value)
            {
                return;
            }

            switch (this.sfd.FilterIndex)
            {
                case 1:
                    using (var sw = new System.IO.StreamWriter(this.sfd.OpenFile()))
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
                    using (var sw = new System.IO.StreamWriter(this.sfd.OpenFile()))
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
                    using (var bw = new System.IO.BinaryWriter(this.sfd.OpenFile()))
                    {
                        var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                        if (!(this.imgGraph.Source is BitmapImage bitmapImage))
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
                        bw.Write(this.prop);
                        bw.Write(this.zoom);

                        //X axis
                        bw.Write(this.Axes.Xmin.X); bw.Write(this.Axes.Xmin.Y); bw.Write(this.Axes.Xmin.Value);
                        bw.Write(this.Axes.Xmax.X); bw.Write(this.Axes.Xmax.Y); bw.Write(this.Axes.Xmax.Value);
                        bw.Write(this.Axes.XLog);

                        //Y axis
                        bw.Write(this.Axes.Ymin.X); bw.Write(this.Axes.Ymin.Y); bw.Write(this.Axes.Ymin.Value);
                        bw.Write(this.Axes.Ymax.X); bw.Write(this.Axes.Ymax.Y); bw.Write(this.Axes.Ymax.Value);
                        bw.Write(this.Axes.YLog);

                        //Points
                        bw.Write(this.data.Count);
                        foreach (var p in this.data)
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
            this.prop = 100;

            this.imgGraph.Width = bmp.PixelWidth;
            this.imgGraph.Height = bmp.PixelHeight;
            this.imgGraph.Source = bmp;
            this.cnvGraph.Width = bmp.PixelWidth;
            this.cnvGraph.Height = bmp.PixelHeight;

            this.imgZoom.Width = bmp.PixelWidth * this.zoom;
            this.imgZoom.Height = bmp.PixelHeight * this.zoom;
            this.imgZoom.Source = bmp;

            this.viewModel.State = State.Axes;
            this.Axes.Status = 0;

            this.DeletePoints();
            if (this.Axes.Xaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Xaxis);
            }

            if (this.Axes.Yaxis != null)
            {
                this.cnvGraph.Children.Remove(this.Axes.Yaxis);
            }

            this.Axes.Xmin.X = this.Axes.Xmin.Y = this.Axes.Xmax.X = this.Axes.Xmax.Y = this.Axes.Ymin.X = this.Axes.Ymin.Y = this.Axes.Ymax.X = this.Axes.Ymax.Y = double.NaN;

            this.SetToolTip();
            this.viewModel.CanvasCursor = Cursors.Cross;
        }

        // TODO: All code below should be removed once MVVM is fully implemented
        
        private Axes Axes
        {
            get => this.viewModel.Axes;
            set => this.viewModel.Axes = value;
        }

        private void SetToolTip()
        {
            // TODO: Make SetToolTip() private after removing this
            this.viewModel.SetToolTip();
        }
    }
}
