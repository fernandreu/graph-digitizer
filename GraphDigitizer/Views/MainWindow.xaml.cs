using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT p);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ClipCursor")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool ClipCursor(ref RECT r);

        bool Prec = false; //Precision mode
        Point PrevPos; //Previous position
        State state = State.Idle;
        Axes axes;
        List<DataPoint> data;

        //Zoom properties, prop in percentage
        int zoom = 2, prop = 100; 
        
        //Selection rectangle
        bool Selecting = false; //Selection rectangle off
        Point SelFirstPos; //First rectangle corner
        Rectangle SelRect; //SelectionRectangle

        //Help window
        Help HelpWindow;

        OpenFileDialog ofd;
        SaveFileDialog sfd;

        public MainWindow()
        {
            this.InitializeComponent();
            this.data = new List<DataPoint>();
            this.dgrPoints.ItemsSource = this.data;
            this.axes.Xmin.Value = 0.0;
            this.axes.Xmax.Value = 1.0;
            this.axes.Ymin.Value = 0.0;
            this.axes.Ymax.Value = 1.0;
            this.zoom = Properties.Settings.Default.Zoom;
            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
                this.OpenFile(Properties.Settings.Default.LastFile);
            this.UpdateProportions((double)Properties.Settings.Default.Proportion);

            this.ofd = new OpenFileDialog();
            this.ofd.FileName = "";
            this.ofd.Filter = "Image files|*.bmp;*.gif;*.tiff;*.jpg;*.jpeg;*.png|Graph Digitizer files|*.gdf";

            this.sfd = new SaveFileDialog();
            this.sfd.FileName = "";
            this.sfd.Filter = "Text files|*.txt|CSV files|*.csv|Graph Digitizer Files|*.gdf";
        }

        private void OpenFile(string path)
        {
            BitmapImage bmp = new BitmapImage(new Uri(path));

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

            this.state = State.Axes;
            this.axes.Status = 0;

            this.DeletePoints();
            if (this.axes.Xaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Xaxis);
            if (this.axes.Yaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Yaxis);

            this.axes.Xmin.X = this.axes.Xmin.Y = this.axes.Xmax.X = this.axes.Xmax.Y = this.axes.Ymin.X = this.axes.Ymin.Y = this.axes.Ymax.X = this.axes.Ymax.Y = double.NaN;

            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!this.ofd.ShowDialog().Value) return;
            if (this.ofd.FilterIndex == 1)
            {
                this.OpenFile(this.ofd.FileName);
                Properties.Settings.Default.LastFile = this.ofd.FileName;
                Properties.Settings.Default.Save();
            }
            else if (this.ofd.FilterIndex == 2)
            {
                System.IO.BinaryReader br = new System.IO.BinaryReader(this.ofd.OpenFile());
                System.Globalization.CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                BitmapImage bmp = this.ImageFromBuffer(br.ReadBytes(br.ReadInt32()));

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

                this.axes.Xmin.X = br.ReadDouble(); this.axes.Xmin.Y = br.ReadDouble(); this.axes.Xmin.Value = br.ReadDouble();
                this.axes.Xmax.X = br.ReadDouble(); this.axes.Xmax.Y = br.ReadDouble(); this.axes.Xmax.Value = br.ReadDouble();
                this.axes.XLog = br.ReadBoolean();
                this.CreateXaxis();

                this.axes.Ymin.X = br.ReadDouble(); this.axes.Ymin.Y = br.ReadDouble(); this.axes.Ymin.Value = br.ReadDouble();
                this.axes.Ymax.X = br.ReadDouble(); this.axes.Ymax.Y = br.ReadDouble(); this.axes.Ymax.Value = br.ReadDouble();
                this.axes.YLog = br.ReadBoolean();
                this.CreateYaxis();

                this.DeletePoints();
                int total = br.ReadInt32();
                this.data.Capacity = total;
                for (int i = 0; i < total; i++)
                    this.data.Add(new DataPoint(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), this.cnvGraph, this.prop, i + 1));
                this.dgrPoints.Items.Refresh();

                this.state = State.Points;
                this.SetToolTip();
                this.cnvGraph.Cursor = Cursors.Cross;

                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        private void SetToolTip()
        {
            switch (this.state)
            {
                case State.Idle:
                    this.txtToolTip.Text = "Load an image with the button above";
                    break;
                case State.Axes:
                    switch (this.axes.Status)
                    {
                        case 0:
                            this.txtToolTip.Text = "Select the minor X value of the axes";
                            break;
                        case 1:
                            this.txtToolTip.Text = "Select the major X value of the axes";
                            break;
                        case 2:
                            this.txtToolTip.Text = "Select the minor Y value of the axes";
                            break;
                        case 3:
                            this.txtToolTip.Text = "Select the major Y value of the axes";
                            break;
                    }
                    break;
                case State.Points:
                    this.txtToolTip.Text = "Select any point you want to add to the data";
                    break;
                case State.Select:
                    this.txtToolTip.Text = "Click on any point to select it";
                    break;
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {

            Point p = e.GetPosition(this.imgGraph);
            if (this.Selecting) //Update selection rectangle position
            {
                if (p.X > this.SelFirstPos.X)
                    this.SelRect.Width = p.X - this.SelFirstPos.X;
                else
                {
                    Canvas.SetLeft(this.SelRect, p.X);
                    this.SelRect.Width = this.SelFirstPos.X - p.X;
                }

                if (p.Y > this.SelFirstPos.Y)
                    this.SelRect.Height = p.Y - this.SelFirstPos.Y;
                else
                {
                    Canvas.SetTop(this.SelRect, p.Y);
                    this.SelRect.Height = this.SelFirstPos.Y - p.Y;
                }
            }
            p.X *= 100.0 / this.prop;
            p.Y *= 100.0 / this.prop;
            this.UpdateStatusCoords(p.X, p.Y);
            Canvas.SetLeft(this.imgZoom, 100 - p.X * this.zoom);
            Canvas.SetTop(this.imgZoom, 100 - p.Y * this.zoom);

            if (this.state == State.Axes)
            {
                if (this.axes.Status == 1)
                {
                    this.axes.Xaxis.X2 = p.X;
                    this.axes.Xaxis.Y2 = p.Y;
                }
                else if (this.axes.Status == 3)
                {
                    this.axes.Yaxis.X2 = p.X;
                    this.axes.Yaxis.Y2 = p.Y;
                }
            }
        }

        private void UpdateStatusCoords(double X, double Y)
        {
            double xreal, yreal;
            this.GetRealCoords(X, Y, out xreal, out yreal);
            this.txtScreenX.Text = (X * 100 / this.prop).ToString("F2");
            this.txtScreenY.Text = (Y * 100 / this.prop).ToString("F2");
            this.txtRealX.Text = FormatNum(xreal);
            this.txtRealY.Text = FormatNum(yreal);
        }

        public static string FormatNum(double num)
        {
            if (double.IsNaN(num)) return "N/A";
            if (num == 0.0) return "0";

            double aux = Math.Abs(num); int dig;
            if (aux > 999999.0 || aux < 0.00001)
                return num.ToString("E4");
            dig = (int)(Math.Log10(aux)+Math.Sign(Math.Log10(aux)));
            if (dig >= 0)
                return num.ToString(string.Format("F{0}", 8 - dig));
            else
                return num.ToString("F8");
        }

        private void cnvZoom_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this.imgZoom);
            this.UpdateStatusCoords(p.X / this.zoom, p.Y / this.zoom);
        }

        private void ZoomModeIn()
        {
            Point p; POINT prev; RECT r;
            this.Prec = true;
            GetCursorPos(out prev);
            this.PrevPos.X = (double)prev.X;
            this.PrevPos.Y = (double)prev.Y;
            p = this.PointToScreen(this.cnvZoom.TransformToAncestor(this).Transform(new Point(0, 0)));
            SetCursorPos((int)p.X + 100, (int)p.Y + 100);
            r.Top = (int)p.Y;
            r.Bottom = (int)p.Y + 200;
            r.Left = (int)p.X;
            r.Right = (int)p.X + 200;
            ClipCursor(ref r);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && this.cnvGraph.IsMouseOver)
                this.ZoomModeIn();
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                return;
            switch (e.Key)
            {
                case Key.F1:
                    this.btnHelp_Click(sender, e);
                    break;
                case Key.D1:
                    this.btnSelect_Click(sender, e);
                    break;
                case Key.D2:
                    this.btnPoints_Click(sender, e);
                    break;
            }
        }

        private void ZoomModeOut(bool recover = true)
        {
            RECT r;
            this.Prec = false;
            r.Top = 0;
            r.Bottom = int.MaxValue;
            r.Left = 0;
            r.Right = int.MaxValue;
            ClipCursor(ref r);
            if (recover) SetCursorPos((int)this.PrevPos.X, (int)this.PrevPos.Y);
        }

        private void OnWindowPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && this.Prec)
                this.ZoomModeOut();
        }

        public void GetRealCoords(double X, double Y, out double RealX, out double RealY)
        {
            double Xaxis, Yaxis;
            //First: obtain the equivalent point in the X axis and in the Y axis
            Xaxis = -((this.axes.Ymax.X - this.axes.Ymin.X) * (this.axes.Xmax.X * this.axes.Xmin.Y - this.axes.Xmax.Y * this.axes.Xmin.X) - (this.axes.Xmax.X - this.axes.Xmin.X) * (X * (this.axes.Ymin.Y - this.axes.Ymax.Y) + Y * (this.axes.Ymax.X - this.axes.Ymin.X))) / ((this.axes.Xmax.Y - this.axes.Xmin.Y) * (this.axes.Ymax.X - this.axes.Ymin.X) - (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y));
            Yaxis = (Y * (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y) + (this.axes.Xmax.Y - this.axes.Xmin.Y) * (this.axes.Ymax.Y * this.axes.Ymin.X - this.axes.Ymax.X * this.axes.Ymin.Y + X * (this.axes.Ymin.Y - this.axes.Ymax.Y))) / ((this.axes.Xmin.Y - this.axes.Xmax.Y) * (this.axes.Ymax.X - this.axes.Ymin.X) + (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y));

            if (this.axes.XLog)
                RealX = Math.Pow(10.0, Math.Log10(this.axes.Xmin.Value) + (Xaxis - this.axes.Xmin.X) / (this.axes.Xmax.X - this.axes.Xmin.X) * (Math.Log10(this.axes.Xmax.Value) - Math.Log10(this.axes.Xmin.Value)));
            else
                RealX = this.axes.Xmin.Value + (Xaxis - this.axes.Xmin.X) / (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Xmax.Value - this.axes.Xmin.Value);

            if (this.axes.YLog)
                RealY = Math.Pow(10.0, Math.Log10(this.axes.Ymin.Value) + (Yaxis - this.axes.Ymin.Y) / (this.axes.Ymax.Y - this.axes.Ymin.Y) * (Math.Log10(this.axes.Ymax.Value) - Math.Log10(this.axes.Ymin.Value)));
            else
                RealY = this.axes.Ymin.Value + (Yaxis - this.axes.Ymin.Y) / (this.axes.Ymax.Y - this.axes.Ymin.Y) * (this.axes.Ymax.Value - this.axes.Ymin.Value);
        }

        private void AddPoint(double X, double Y)
        {
            double Xpoint, Ypoint;
            this.GetRealCoords(X, Y, out Xpoint, out Ypoint);
            this.data.Add(new DataPoint(Xpoint, Ypoint, X, Y, this.cnvGraph, this.prop, this.data.Count + 1));
            this.data[this.data.Count - 1].Obj.MouseDown += new MouseButtonEventHandler(this.PointMouseDown);
            this.dgrPoints.Items.Refresh();
        }

        private void CreateXaxis()
        {
            if (this.axes.Xaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Xaxis);
            this.axes.Xaxis = new Line();
            this.axes.Xaxis.X1 = this.axes.Xmin.X * this.prop / 100;
            this.axes.Xaxis.Y1 = this.axes.Xmin.Y * this.prop / 100;
            this.axes.Xaxis.X2 = this.axes.Xmax.X * this.prop / 100;
            this.axes.Xaxis.Y2 = this.axes.Xmax.Y * this.prop / 100;
            this.axes.Xaxis.Stroke = Brushes.Red;
            this.axes.Xaxis.StrokeThickness = 2;
            this.axes.Xaxis.StrokeDashArray = new DoubleCollection();
            this.axes.Xaxis.StrokeDashArray.Add(5.0);
            this.axes.Xaxis.StrokeDashArray.Add(5.0);
            this.axes.Xaxis.StrokeEndLineCap = PenLineCap.Triangle;
            this.cnvGraph.Children.Add(this.axes.Xaxis);
        }

        private void CreateYaxis()
        {
            if (this.axes.Yaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Yaxis);
            this.axes.Yaxis = new Line();
            this.axes.Yaxis.X1 = this.axes.Ymin.X * this.prop / 100;
            this.axes.Yaxis.Y1 = this.axes.Ymin.Y * this.prop / 100;
            this.axes.Yaxis.X2 = this.axes.Ymax.X * this.prop / 100;
            this.axes.Yaxis.Y2 = this.axes.Ymax.Y * this.prop / 100;
            this.axes.Yaxis.Stroke = Brushes.Blue;
            this.axes.Yaxis.StrokeThickness = 2;
            this.axes.Yaxis.StrokeDashArray = new DoubleCollection();
            this.axes.Yaxis.StrokeDashArray.Add(5.0);
            this.axes.Yaxis.StrokeDashArray.Add(5.0);
            this.axes.Yaxis.StrokeEndLineCap = PenLineCap.Round;
            this.cnvGraph.Children.Add(this.axes.Yaxis);
        }

        private void SelectPoint(double X, double Y)
        {
            if (this.state == State.Axes)
            {
                switch (this.axes.Status)
                {
                    case 0: //Xmin
                        this.axes.Xmin.X = X;
                        this.axes.Xmin.Y = Y;
                        this.axes.Xmax.X = X;
                        this.axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 1:
                        this.axes.Xmax.X = X;
                        this.axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 2:
                        this.axes.Ymin.X = X;
                        this.axes.Ymin.Y = Y;
                        this.axes.Ymax.X = X;
                        this.axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                    case 3:
                        this.axes.Ymax.X = X;
                        this.axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                }
                this.axes.Status++;
                if (this.axes.Status == 4)
                    this.SelectAxesProp();
            }
            else if (this.state == State.Points)
            {
                this.AddPoint(X, Y);
            }
            this.SetToolTip();
        }

        private void UpdateData()
        {
            for (int i = 0; i < this.data.Count; i++)
                this.data[i].Obj.Content = (i + 1) % 100;
            this.dgrPoints.Items.Refresh();
        }

        private void PointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.state != State.Select) return;
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
            Point p = e.GetPosition(this.imgGraph);
            if (this.state == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                this.Selecting = true;
                this.SelFirstPos = p;
                this.SelRect = new Rectangle() { Stroke = new SolidColorBrush(new Color() { ScA = 0.7f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), Fill = new SolidColorBrush(new Color() { ScA = 0.2f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), StrokeThickness = 1.0 };
                this.cnvGraph.Children.Add(this.SelRect);
                Canvas.SetLeft(this.SelRect, this.SelFirstPos.X);
                Canvas.SetTop(this.SelRect, this.SelFirstPos.Y);
            }
            else
                this.SelectPoint(p.X / this.prop * 100, p.Y / this.prop * 100);
        }

        private void imgZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this.imgZoom);
            this.SelectPoint(p.X / this.zoom, p.Y / this.zoom);
        }

        private void btnAxes_Click(object sender, RoutedEventArgs e)
        {
            this.state = State.Axes;
            this.axes.Status = 0;
            this.SetToolTip();
        }

        private void btnAxesProp_Click(object sender, RoutedEventArgs e)
        {
            this.SelectAxesProp();
        }

        private void SelectAxesProp()
        {
            if (this.Prec) this.ZoomModeOut(false);
            AxesProp ap = new AxesProp(this.axes);
            POINT p;
            GetCursorPos(out p);
            //The program will try to position the window leaving the mouse in a corner
            if (p.X + ap.Width > SystemParameters.PrimaryScreenWidth)
                ap.Left = p.X - ap.Width + 20;
            else
                ap.Left = p.X;

            if (p.Y + ap.Height > SystemParameters.PrimaryScreenHeight - 50) //Thresold for the Windows taskbar
                ap.Top = p.Y - ap.Height;
            else
                ap.Top = p.Y;

            ap.ShowDialog();
            this.axes = ap.axes;
            this.state = State.Points;
        }

        private void DeletePoints()
        {
            foreach (DataPoint dp in this.data)
                this.cnvGraph.Children.Remove(dp.Obj);
            
            this.data.Clear();
            this.dgrPoints.Items.Refresh();
        }

        private void btnDelPoints_Click(object sender, RoutedEventArgs e)
        {
            this.DeletePoints();
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (this.zoom < 16) this.zoom *= 2;
            this.imgZoom.Width = ((BitmapSource)this.imgZoom.Source).PixelWidth * this.zoom;
            this.imgZoom.Height = ((BitmapSource)this.imgZoom.Source).PixelHeight * this.zoom;
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
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
            for (int i = 1; i < this.cnvGraph.Children.Count; i++) //0 Index always for the imgGraph element
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

        private void btnEnlarge_Click(object sender, RoutedEventArgs e)
        {
            if (this.prop >= 16000) return;
            double newprop = Math.Floor(this.prop * 1.2);
            if (Math.Abs(newprop - 100.0) < 1.0) newprop = 100.0;
            this.UpdateProportions(newprop);
        }

        private void btnReduce_Click(object sender, RoutedEventArgs e)
        {
            if (this.prop <= 5) return;
            double newprop = Math.Floor(this.prop / 1.2);
            if (Math.Abs(newprop - 100.0) < 1.0) newprop = 100.0;
            this.UpdateProportions(newprop);
            this.prop = (int)newprop;
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
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

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            string res = "";
            for (int i = 0; i < this.data.Count; i++)
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
            if (!this.Selecting)
            {
                return;
            }

            if (double.IsNaN(this.SelRect.Width) || this.SelRect.Width < 1.0 || double.IsNaN(this.SelRect.Height) || this.SelRect.Height < 1.0)
            {
                //Nothing at the moment
            }
            else
            {
                double left = Canvas.GetLeft(this.SelRect), top = Canvas.GetTop(this.SelRect), x, y;
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
                        if (x >= left && x <= left + this.SelRect.Width && y >= top && y <= top + this.SelRect.Height) //Point is within the rectangle
                            this.dgrPoints.SelectedItems.Add((DataPoint)tb.Tag);
                    }
                }
            }
            this.cnvGraph.Children.Remove(this.SelRect);
            this.Selecting = false;
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (this.HelpWindow == null || !this.HelpWindow.IsLoaded)
            {
                this.HelpWindow = new Help();
                this.HelpWindow.Show();
            }
            else
            {
                this.HelpWindow.Focus();
                if (this.HelpWindow.WindowState == WindowState.Minimized)
                    this.HelpWindow.WindowState = WindowState.Normal;
            }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            this.state = State.Select;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Arrow;
        }

        private void btnPoints_Click(object sender, RoutedEventArgs e)
        {
            this.state = State.Points;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!this.sfd.ShowDialog().Value) return;
            System.IO.StreamWriter sw;
            System.IO.BinaryWriter bw;
            switch (this.sfd.FilterIndex)
            {
                case 1:
                    sw = new System.IO.StreamWriter(this.sfd.OpenFile());
                    sw.WriteLine("{0,-22}{1,-22}", "X Value", "Y Value");
                    sw.WriteLine(new string('-', 45));
                    for (int i = 0; i < this.data.Count; i++)
                        sw.WriteLine("{0,-22}{1,-22}", this.data[i].X, this.data[i].Y);
                    sw.Close();
                    break;
                case 2:
                    sw = new System.IO.StreamWriter(this.sfd.OpenFile());
                    string sep = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                    sw.WriteLine("X Value" + sep + "Y Value");
                    for (int i = 0; i < this.data.Count; i++)
                        sw.WriteLine(this.data[i].X.ToString() + sep + this.data[i].Y.ToString());
                    sw.Close();
                    break;
                case 3:
                    bw = new System.IO.BinaryWriter(this.sfd.OpenFile());
                    System.Globalization.CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                    byte[] bmp = this.BufferFromImage((BitmapImage)this.imgGraph.Source);
                    bw.Write(bmp.Length);
                    bw.Write(bmp);

                    //Proportion and zoom
                    bw.Write(this.prop);
                    bw.Write(this.zoom);

                    //X axis
                    bw.Write(this.axes.Xmin.X); bw.Write(this.axes.Xmin.Y); bw.Write(this.axes.Xmin.Value);
                    bw.Write(this.axes.Xmax.X); bw.Write(this.axes.Xmax.Y); bw.Write(this.axes.Xmax.Value);
                    bw.Write(this.axes.XLog);

                    //Y axis
                    bw.Write(this.axes.Ymin.X); bw.Write(this.axes.Ymin.Y); bw.Write(this.axes.Ymin.Value);
                    bw.Write(this.axes.Ymax.X); bw.Write(this.axes.Ymax.Y); bw.Write(this.axes.Ymax.Value);
                    bw.Write(this.axes.YLog);

                    //Points
                    bw.Write(this.data.Count);
                    for (int i = 0; i < this.data.Count; i++)
                    {
                        bw.Write(this.data[i].X);
                        bw.Write(this.data[i].Y);
                        bw.Write(this.data[i].RealX);
                        bw.Write(this.data[i].RealY);
                    }
                    bw.Close();
                    System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    break;
            }
        }

        public BitmapImage ImageFromBuffer(Byte[] bytes)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream(bytes);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }

        public Byte[] BufferFromImage(BitmapImage imageSource)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            PngBitmapEncoder enc = new PngBitmapEncoder();
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

            BitmapSource bmp = Clipboard.GetImage();

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

            this.state = State.Axes;
            this.axes.Status = 0;

            this.DeletePoints();
            if (this.axes.Xaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Xaxis);
            if (this.axes.Yaxis != null)
                this.cnvGraph.Children.Remove(this.axes.Yaxis);

            this.axes.Xmin.X = this.axes.Xmin.Y = this.axes.Xmax.X = this.axes.Xmax.Y = this.axes.Ymin.X = this.axes.Ymin.Y = this.axes.Ymax.X = this.axes.Ymax.Y = double.NaN;

            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

    }

    public enum State
    {
        Idle,
        Axes,
        Select,
        Points
    }

    public struct POINT
    {
        public int X, Y;
    }

    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public struct Coord
    {
        public double X, Y, Value;
    }

    public struct Axes
    {
        public Coord Xmin, Xmax, Ymin, Ymax;
        public int Status; //Which is the next point to assign, in the order above
        public bool XLog, YLog; //If the axes are logarithmic or not
        public Line Xaxis, Yaxis;
    }

    public class DataPoint
    {
        public double X { get; set; } //The point in transformed coordinates
        public double Y { get; set; }
        public double RealX { get; set; } //The point in screen coordinates
        public double RealY { get; set; }
        public Label Obj { get; set; } //Label containing the point element on the graph
        public string Xform { get; set; } //Formatted X and Y
        public string Yform { get; set; }

        public DataPoint(double x, double y, double realx, double realy, Canvas owner, int proportion, int pos)
        {
            this.X = x;
            this.Y = y;
            this.RealX = realx;
            this.RealY = realy;
            this.Xform = MainWindow.FormatNum(x);
            this.Yform = MainWindow.FormatNum(y);
            this.Obj = new Label() { Content = pos % 100, Style = (Style)Application.Current.FindResource("PointStyle") };
            int index = owner.Children.Add(this.Obj);
            Canvas.SetLeft(owner.Children[index], realx * proportion / 100 - 8);
            Canvas.SetTop(owner.Children[index], realy * proportion / 100 - 8);
            this.Obj.Tag = this;
        }
    }

    public class Commands
    {
        public static RoutedCommand Help;

        static Commands()
        {
            Help = new RoutedCommand("Help", typeof(Commands));
            Help.InputGestures.Add(new KeyGesture(Key.F1));
        }
    }
}
