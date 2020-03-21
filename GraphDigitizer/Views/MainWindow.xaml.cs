using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using GraphDigitizer.Models;
using GraphDigitizer.ViewModels;
using GraphDigitizer.ViewModels.Graphics;

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

        public MainWindow()
        {
            InitializeComponent();

            viewModel = (MainWindowViewModel) DataContext;
            viewModel.ZoomModeEnter += (o, e) => ZoomModeIn();
            viewModel.ZoomModeLeave += (o, e) => ZoomModeOut();

            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                viewModel.OpenFile(Properties.Settings.Default.LastFile);
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {
            var graph = (FrameworkElement)sender;
            var p = e.GetPosition(graph);
            if (selecting) //Update selection rectangle position
            {
                if (p.X > selFirstPos.X)
                    viewModel.SelectionRectangle.Width = (p.X - selFirstPos.X) / graph.ActualWidth;
                else
                {
                    viewModel.SelectionRectangle.Left = p.X / graph.ActualWidth;
                    viewModel.SelectionRectangle.Width = (selFirstPos.X - p.X) / graph.ActualWidth;
                }

                if (p.Y > selFirstPos.Y)
                    viewModel.SelectionRectangle.Height = (p.Y - selFirstPos.Y) / graph.ActualHeight;
                else
                {
                    viewModel.SelectionRectangle.Top = p.Y / graph.ActualHeight;
                    viewModel.SelectionRectangle.Height = (selFirstPos.Y - p.Y) / graph.ActualHeight;
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
            var p = PointToScreen(ZoomCanvas.TransformToAncestor(this).Transform(new Point(0, 0)));
            MouseUtils.SetCursorPos((int)(p.X + ZoomCanvas.ActualWidth / 2), (int)(p.Y + ZoomCanvas.ActualHeight / 2));

            MouseUtils.Rect r;
            r.Top = (int)p.Y;
            r.Bottom = (int)(p.Y + ZoomCanvas.ActualHeight);
            r.Left = (int)p.X;
            r.Right = (int)(p.X + ZoomCanvas.ActualWidth);
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
            var graph = (FrameworkElement)sender;
            var p = e.GetPosition(DataCanvas);
            if (viewModel.State == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                selecting = true;
                selFirstPos = p;
                viewModel.SelectionRectangle = new ViewModels.Graphics.Rectangle
                {
                    Left = selFirstPos.X / graph.ActualWidth,
                    Top = selFirstPos.Y / graph.ActualHeight,
                };
            }
            else
                viewModel.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(DataCanvas.ActualWidth, DataCanvas.ActualHeight));
        }

        private void ZoomMouseDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(ZoomImage);
            viewModel.SelectPoint(new AbsolutePoint(p.X, p.Y).ToRelative(ZoomImage.ActualWidth, ZoomImage.ActualHeight));
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Zoom = viewModel.Zoom;
            Properties.Settings.Default.Proportion = viewModel.CanvasFactor;
            Properties.Settings.Default.Save();
        }

        private void OnWindowPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!selecting)
            {
                return;
            }

            var selRect = viewModel.SelectionRectangle;
            var left = selRect.Left;
            var top = selRect.Top;
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                viewModel.SelectedData.Clear();
            }

            foreach (var point in viewModel.Data)
            {
                if (viewModel.SelectedData.Contains(point))
                {
                    continue;
                }

                var x = point.Relative.X;
                var y = point.Relative.Y;
                if (x >= left && x <= left + selRect.Width && y >= top && y <= top + selRect.Height)
                {
                    viewModel.SelectedData.Add(point);
                }
            }

            viewModel.SelectionRectangle = null;
            selecting = false;
        }

        private void MouseEnterEventHandler(object sender, MouseEventArgs e)
        {
            viewModel.IsMouseOverCanvas = true;
        }

        private void MouseLeaveEventHandler(object sender, MouseEventArgs e)
        {
            viewModel.IsMouseOverCanvas = false;
        }
    }
}
