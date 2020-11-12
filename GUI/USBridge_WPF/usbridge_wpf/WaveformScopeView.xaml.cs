using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using INTAN_RHD2000;
using NUnit.Framework;
using SharpDX.WPF;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for DataPlotter.xaml
    /// </summary>
    public partial class WaveformScopeView
    {
        private void ButtonBase_Onclick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("W:" + Plotter2D.ActualWidth + ",H:" + Plotter2D.ActualHeight);
        }

        public FPS FPS { get; set; }

        public uint ChannelId { get; set; }
        public uint AddressId { get; set; }

        public static float Tbase { get; set; }
        public static float Zoom { get; set; }

        public static float PlotHeight { get; set; }

        public static float PlotterWidth { get; set; }

        public static float PlotterHeight { get; set; }

        public static int EstRecordLength = 0;

        //public ulong FrameBeginTime;

        //List<ChannelLabel> _channelLabel = new List<ChannelLabel>();

        public WaveformScope SignalPlotter; //, SpikePLotter;
        private ObservableCollection<ChannelLabel> _channelLabel = new ObservableCollection<ChannelLabel>();

        public ObservableCollection<ChannelLabel> ChannelLabel
        {
            get { return _channelLabel ?? (_channelLabel = new ObservableCollection<ChannelLabel>()); }
            set { _channelLabel = value; }
        }


        private static ObservableCollection<VoltageRange> _voltageRangeList;

        public static ObservableCollection<VoltageRange> VoltageRangeList
        {
            get
            {
                return _voltageRangeList ?? (_voltageRangeList = new ObservableCollection<VoltageRange>
                {
                    new VoltageRange() {Value = 30000, Disp = "3 mV/Div"},
                    new VoltageRange() {Value = 20000, Disp = "2 mV/Div"},
                    new VoltageRange() {Value = 10000, Disp = "1 mV/Div"},
                    new VoltageRange() {Value = 5000, Disp = "500 uV/Div"},
                    new VoltageRange() {Value = 3000, Disp = "300 uV/Div"},
                    new VoltageRange() {Value = 2000, Disp = "200 uV/Div"},
                    new VoltageRange() {Value = 1500, Disp = "150 uV/Div"},
                    new VoltageRange() {Value = 1000, Disp = "100 uV/Div"},
                    new VoltageRange() {Value = 500, Disp = "50 uV/Div"},
                    new VoltageRange() {Value = 250, Disp = "25 uV/Div"},
                    new VoltageRange() {Value = 200, Disp = "20 uV/Div"},
                    new VoltageRange() {Value = 150, Disp = "15 uV/Div"},
                    new VoltageRange() {Value = 100, Disp = "10 uV/Div"},
                    new VoltageRange() {Value = 50, Disp = "5 uV/Div"},
                });
            }
        }

        private int _voltageRange;

        public int VoltageRange
        {
            get { return _voltageRange; }
            set
            {
                if (_voltageRange == value) return;
                _voltageRange = value;
                NotifyPropertyChange("VoltageRange");
            }
        }


        private static ObservableCollection<TimeRange> _timeSpanList;

        public static ObservableCollection<TimeRange> TimeSpanList
        {
            get
            {
                return _timeSpanList ?? (_timeSpanList = new ObservableCollection<TimeRange>
                {
                    new TimeRange() {Value = 50, Disp = "5 ms/Div"},
                    new TimeRange() {Value = 100, Disp = "10 ms/Div"},
                    new TimeRange() {Value = 250, Disp = "25 ms/Div"},
                    new TimeRange() {Value = 500, Disp = "50 ms/Div"},
                    new TimeRange() {Value = 1000, Disp = "100 ms/Div"},
                    new TimeRange() {Value = 2000, Disp = "200 ms/Div"},
                    new TimeRange() {Value = 5000, Disp = "500 ms/Div"},
                    new TimeRange() {Value = 10000, Disp = "1 s/Div"},
                });
            }
        }

        private int _timeSpan;

        public int TimeSpan
        {
            get { return _timeSpan; }
            set
            {
                if (_timeSpan == value) return;
                _timeSpan = value;
                NotifyPropertyChange("TimeSpan");
            }
        }


        private int _displayAddr = MainWindow.CurrentDeviceAddress;

        public int DisplayAddr
        {
            get { return _displayAddr; }
            set
            {
                if (_displayAddr == value) return;
                _displayAddr = value;
                NotifyPropertyChange("DisplayAddr");
            }
        }


        //public int GetDisplayAddr()
        //{
        //    return DisplayAddr;
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(p));
        }


        public WaveformScopeView(uint addressId, uint channelId)
        {
            //FPS = new FPS();
            AddressId = addressId;
            ChannelId = channelId;

            // Initialise data binding

            VoltageRange = 150;
            TimeSpan = 1000;
            PlotHeight = 22.5f;
            //FrameBeginTime = (MainWindow.RunningTime / 32 / ((ulong) MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate / 1000) / (ulong) TimeSpan) * (ulong) TimeSpan;
            //FrameBeginTime = ((MainWindow.RunningTime /32 ) /
            //                  ((ulong) MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate * (ulong) TimeSpan / 1000)) 
            //                  *
            //                 ((ulong) MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate * (ulong) TimeSpan / 1000);    // Find the frame begin time in unit of samples.
            InitializeComponent();
            update_Labels();

            base.Loaded += OnContentRendered;
                // Non-Window doesn't have OnContentRenderMethod. Has to trace back to the base class event.
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            #region WaveformScope

            DrawAxis();
            SignalPlotter = new WaveformScope(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate,
                (uint) TimeSpan,
                VoltageRange,
                Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                AddressId, ChannelId
                );
            MainWindow.IsSpikeSortingChanged += SwitchScopeMode;
            // Logic
            Plotter2D.Renderer = SignalPlotter;
            TimeSpanBox.SelectionChanged += ViewArea_SelectionChanged;
            VoltageRangeBox.SelectionChanged += ViewArea_SelectionChanged;

            #endregion
        }

        public void SwitchScopeMode(object sender, EventArgs e)
        {
            RecreatePlotter();
        }

        private void DrawAxis()
        {
            if (PlotterAxis.Children.Count != 0) PlotterAxis.Children.Clear();

            var dashes = new DoubleCollection {2, 2};
            int divisions = 10;
            double scaling = 0.8;
            // Draw Dotted Lines#
            for (int i = 0; i < divisions+1; i++) //Serve as voltage scales
            {
                //var splitter = new Line();

                //splitter.Stroke = Brushes.Gray;
                //splitter.Opacity = 0.5;
                //splitter.X1 = 0;
                //splitter.X2 = PlotterAxis.ActualWidth;
                //splitter.Y1 = splitter.Y2 = i * PlotterAxis.ActualHeight / divisions;
                //splitter.StrokeThickness = 1;
                //splitter.StrokeDashArray = dashes;
                //splitter.StrokeDashCap = PenLineCap.Round;
                //RenderOptions.SetCachingHint(splitter, CachingHint.Cache);
                //RenderOptions.SetBitmapScalingMode(splitter, BitmapScalingMode.LowQuality);
                //PlotterAxis.Children.Add(splitter);

                var hsplitter = new Line();
                var vsplitter = new Line();

                hsplitter.Stroke = vsplitter.Stroke = Brushes.Gray;
                hsplitter.Opacity = vsplitter.Opacity = 0.5;
                hsplitter.X1 = 0;
                hsplitter.X2 = PlotterAxis.ActualWidth;
                hsplitter.Y1 = hsplitter.Y2 = i*PlotterAxis.ActualHeight * scaling / divisions + PlotterAxis.ActualHeight * (1- scaling);
                vsplitter.X1 = vsplitter.X2 = i*PlotterAxis.ActualWidth/divisions;
                vsplitter.Y2 = PlotterAxis.ActualHeight;
                vsplitter.Y1 = PlotterAxis.ActualHeight * (1 - scaling);
                hsplitter.StrokeThickness = vsplitter.StrokeThickness = 1;
                hsplitter.StrokeDashArray = vsplitter.StrokeDashArray = dashes;
                hsplitter.StrokeDashCap = vsplitter.StrokeDashCap = PenLineCap.Round;
                RenderOptions.SetCachingHint(hsplitter, CachingHint.Cache);
                RenderOptions.SetCachingHint(vsplitter, CachingHint.Cache);

                RenderOptions.SetBitmapScalingMode(hsplitter, BitmapScalingMode.LowQuality);
                RenderOptions.SetBitmapScalingMode(vsplitter, BitmapScalingMode.LowQuality);

                PlotterAxis.Children.Add(hsplitter);
                PlotterAxis.Children.Add(vsplitter);

            }
        }

        private void update_Labels()
        {
            _channelLabel.Clear();
            for (var i = 31; i >= 0; i--)
            {
                //_channelLabel.Add(new ChannelLabel() { height = plotHeight-0.3f, channelNum = i });
                _channelLabel.Add(new ChannelLabel() {Height = (float) Plotter2D.ActualHeight/32, ChannelNum = i});
            }
            //ChannelLabel.ItemsSource = _channelLabel;
        }

        private void UpdateGraph_Click(object sender, RoutedEventArgs e)
        {
            //Tbase = int.Parse(timeBase.Text);
            //TZoom = float.Parse(timeZoom.Text);
            PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
            PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight - 22);
            PlotHeight = Convert.ToSingle(PlotterHeight/32);
            Plotter2D.Render();
        }

        private void ScopeSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
                PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight - 22);
                PlotHeight = Convert.ToSingle(PlotterHeight/32);
                //plotter2D.Render();
                update_Labels();
                DrawAxis();
            }
        }

        private bool isMouseDown = false;
        private float mouseOrigin = 0;

        private void plotter2D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1)
            {
                return;
            }

            isMouseDown = true;
            Mouse.OverrideCursor = Cursors.Hand;
            mouseOrigin = (float) e.GetPosition(Plotter2D).X;
            //MessageBox.Show(Mouse.GetPosition(plotter2D).ToString());
        }


        private void plotter2D_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void plotter2D_MouseLeave(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void plotter2D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float zoomScale = 2f;
            //MessageBox.Show(e.Delta.ToString());
            // Centre around the mouse
            if (e.Delta < 0)
            {
                // Zoom in
                if (Zoom < 62500/PlotterWidth) //Don't zoom in even further when only 1000 points on the screen
                {
                    Tbase = getSafePosition(Tbase + Convert.ToInt32(e.GetPosition(Plotter2D).X/(zoomScale*Zoom)));
                    Zoom *= zoomScale;
                }
            }
            else
            {
                // Zoom out
                if (Zoom > PlotterWidth/EstRecordLength) //Don't zoom out even further when whole record is shown
                {
                    Tbase = getSafePosition(Tbase - Convert.ToInt32(e.GetPosition(Plotter2D).X/(zoomScale*Zoom)));
                    Zoom /= zoomScale;
                }
            }

            //timeZoom.Text = TZoom.ToString();
            Plotter2D.Render();
        }

        private void plotter2D_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (isMouseDown)
                {
                    Tbase = getSafePosition(Tbase + (mouseOrigin + -(float) (e.GetPosition(Plotter2D).X))/Zoom);
                    //timeBase.Text = Tbase.ToString();
                    Plotter2D.Render();
                    mouseOrigin = (float) e.GetPosition(Plotter2D).X;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        private float getSafePosition(float dispTimeBase)
        {
            float safePosition;

            if (dispTimeBase > EstRecordLength - PlotterWidth*0.6f/Zoom)
            {
                safePosition = EstRecordLength - PlotterWidth*0.6f/Zoom;
            }
            else if (dispTimeBase < 0)
            {
                safePosition = 0.0f;
            }
            else
            {
                safePosition = dispTimeBase;
            }

            return safePosition;
        }

        private void ViewArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //plotter2D.IsLoopRendering = false;
            //timePlotter.UpdateTimeSpan(timeSpan);
            //plotter2D.Renderer.Reset(plotter2D.GetDrawEventArgs());
            //plotter2D.IsLoopRendering = true;
            RecreatePlotter();
            //test.Dispose();
            //plotter2D.Renderer = null;
            //test = new LiveView((double)MainWindow.INTAN.samplingRate, timeSpan, voltageRange, plotter2D.ActualWidth, plotter2D.ActualHeight);
            //plotter2D.Renderer = test;
            //plotter2D.UpdateLayout();
        }

        private void RecreatePlotter()
        {
            if (Plotter2D != null)
            {
                Plotter2D.IsLoopRendering = false;
                //Plotter2D_spike.IsLoopRendering = false;
                SignalPlotter.Dispose();
                //SpikePLotter.Dispose();
                Plotter2D.Renderer = null;
                //Plotter2D_spike.Renderer = null;
#if DEBUG_GPU
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
#endif
                //Todo Propagate address, channel parameter and make header to display

                SignalPlotter = new WaveformScope(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate,
                    (uint) TimeSpan, VoltageRange,
                    Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                    AddressId, ChannelId
                    );
                Plotter2D.Renderer = SignalPlotter;
                Plotter2D.IsLoopRendering = true;
            }
        }

        //public void Dispose()
        //{
        //    if (Plotter2D != null)
        //    {
        //        Plotter2D.IsLoopRendering = false;
        //        SignalPlotter.Dispose();
        //        Plotter2D.Renderer = null;
        //        Plotter2D = null;
        //    }
        //    MainWindow.NeuralChannel[AddressId, ChannelId].IsDisplaying = false;
        //    MainWindow.IsSpikeSortingChanged -= SwitchScopeMode;
        //}
    }
}
