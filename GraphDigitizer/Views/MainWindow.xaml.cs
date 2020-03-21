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
                if (viewModel.Axes.Status == 1)
                {
                    viewModel.Axes.X.Maximum = viewModel.MousePosition;
                }
                else if (viewModel.Axes.Status == 3)
                {
                    viewModel.Axes.Y.Maximum = viewModel.MousePosition;
                }
            }
        }

        private void ZoomMouseMoveEventHandler(object sender, MouseEventArgs e)
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

        private void GraphMouseDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this.DataCanvas);
            if (viewModel.State == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                selecting = true;
                selFirstPos = p;
                selRect = new Rectangle
                {
                    Stroke = new SolidColorBrush(new Color { ScA = 0.7f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), 
                    Fill = new SolidColorBrush(new Color { ScA = 0.2f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), 
                    StrokeThickness = 1.0
                };

                // TODO
                //this.cnvGraph.Children.Add(this.selRect);

                //Canvas.SetLeft(this.selRect, this.selFirstPos.X);
                //Canvas.SetTop(this.selRect, this.selFirstPos.Y);
            }
            else
                this.viewModel.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(this.DataCanvas.ActualWidth, this.DataCanvas.ActualHeight));
        }

        private void ZoomMouseDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(ZoomImage);
            this.viewModel.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(ZoomImage.ActualWidth, ZoomImage.ActualHeight));
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Zoom = this.viewModel.Zoom;
            Properties.Settings.Default.Proportion = this.viewModel.CanvasFactor;
            Properties.Settings.Default.Save();
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
