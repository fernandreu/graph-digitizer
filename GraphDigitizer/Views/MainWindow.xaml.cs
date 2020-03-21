using System;
using System.Windows;
using System.Windows.Input;
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

        private static RelativePoint GetPosition(object sender, MouseEventArgs e)
        {
            var graph = (FrameworkElement) sender;
            var p = e.GetPosition(graph);
            return new RelativePoint(p.X / graph.ActualWidth, p.Y / graph.ActualHeight);
        }

        private void GraphMouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            viewModel.HandleMouseMove(GetPosition(sender, e));
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
            var p = GetPosition(sender, e);
            viewModel.HandleMouseDown(p, e.ChangedButton);
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
            viewModel.HandleMouseUp();
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
