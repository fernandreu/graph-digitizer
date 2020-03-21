using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphDigitizer.Events;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;
using GraphDigitizer.ViewModels.Graphics;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel;

        private Point previousPosition;

        //Selection rectangle
        private bool selecting = false; //Selection rectangle off
        private Point selFirstPos; //First rectangle corner
        private Rectangle selRect; //SelectionRectangle

        public MainWindow()
        {
            InitializeComponent();

            viewModel = (MainWindowViewModel) DataContext;
            viewModel.ZoomModeEnter += (o, e) => this.ZoomModeIn();
            viewModel.ZoomModeLeave += (o, e) => this.ZoomModeOut();

            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                this.viewModel.OpenFile(Properties.Settings.Default.LastFile);
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {
            var graph = (FrameworkElement)sender;
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

            viewModel.MousePosition = new RelativePoint(p.X / graph.ActualWidth, p.Y / graph.ActualHeight);

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

        private void cnvZoom_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(ZoomImage);
            viewModel.UpdateStatusCoords(new RelativePoint(p.X / ZoomImage.ActualWidth * viewModel.TargetImage.Width, p.Y / ZoomImage.ActualHeight * viewModel.TargetImage.Height));
        }

        private void ZoomModeIn()
        {
            MouseUtils.GetCursorPos(out var prev);
            previousPosition.X = (double)prev.X;
            previousPosition.Y = (double)prev.Y;
            var p = PointToScreen(this.ZoomCanvas.TransformToAncestor(this).Transform(new Point(0, 0)));
            MouseUtils.SetCursorPos((int)(p.X + this.ZoomCanvas.ActualWidth / 2), (int)(p.Y + this.ZoomCanvas.ActualHeight / 2));

            MouseUtils.Rect r;
            r.Top = (int)p.Y;
            r.Bottom = (int)(p.Y + this.ZoomCanvas.ActualHeight);
            r.Left = (int)p.X;
            r.Right = (int)(p.X + this.ZoomCanvas.ActualWidth);
            MouseUtils.ClipCursor(ref r);
        }

        private void ZoomModeOut(bool recover = true)
        {
            MouseUtils.Rect r;
            r.Top = int.MinValue;
            r.Bottom = int.MaxValue;
            r.Left = int.MinValue;
            r.Right = int.MaxValue;
            MouseUtils.ClipCursor(ref r);
            if (recover) {
                MouseUtils.SetCursorPos((int)previousPosition.X, (int)previousPosition.Y);
            }
        }

        private void SelectPoint(RelativePoint relative)
        {
            if (viewModel.State == State.Axes)
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
                    this.viewModel.SelectAxesProp();
            }
            else if (viewModel.State == State.Points)
            {
                this.viewModel.AddPoint(relative);
            }
            SetToolTip();
        }

        private void PointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is DataPoint point))
            {
                return;
            }

            viewModel.HandlePointMouseDown(point, e.ChangedButton);
        }

        private void DeleteSelection(object sender, EventArgs e)
        {
            viewModel.DeleteSelection();
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
            var p = e.GetPosition(ZoomImage);
            this.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(ZoomImage.ActualWidth, ZoomImage.ActualHeight));
        }

        private void btnAxes_Click(object sender, RoutedEventArgs e)
        {
            viewModel.State = State.Axes;
            Axes.Status = 0;
            SetToolTip();
        }

        private void btnAxesProp_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.SelectAxesProp();
        }

        private void OnDeletePointsClicked(object sender, RoutedEventArgs e)
        {
            this.viewModel.Data.Clear();
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
                //if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                //    viewModel.SelectedData.Clear();
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
