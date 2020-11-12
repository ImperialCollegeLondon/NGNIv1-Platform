using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpDX.WPF;

namespace USBridge_WPF
{
    public class DataPlotterViewModel
    {
            public FPS FPS
            {
                get;
                set;
            }


            public static float Tbase
            {
                get;
                set;
            }
            public static float Zoom
            {
                get;
                set;
            }

            public static float PlotHeight
            {
                get;
                set;
            }

            public static float PlotterWidth
            {
                get;
                set;
            }

            public static float PlotterHeight
            {
                get;
                set;
            }

            public static int EstRecordLength = 0;

            //List<ChannelLabel> _channelLabel = new List<ChannelLabel>();

            public LiveView TimePlotter;
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
                    new VoltageRange() {Value = 1000, Disp = "+/- 500uV"},
                    new VoltageRange() {Value = 800, Disp = "+/- 400uV"},
                    new VoltageRange() {Value = 512, Disp = "+/- 256uV"}
                });
                }
            }
            private static int _voltageRange;
            public static int VoltageRange
            {
                get { return _voltageRange; }
                set
                {
                    if (_voltageRange == value) return;
                    _voltageRange = value;
                    if (VoltageRangeChanged != null) VoltageRangeChanged(null, EventArgs.Empty);
                }
            }
            public static event EventHandler VoltageRangeChanged;

            private static ObservableCollection<TimeRange> _timeSpanList;
            public static ObservableCollection<TimeRange> TimeSpanList
            {
                get
                {
                    return _timeSpanList ?? (_timeSpanList = new ObservableCollection<TimeRange>
                {
                    new TimeRange() {Value = 50, Disp = "50ms"},
                    new TimeRange() {Value = 100, Disp = "100ms"},
                    new TimeRange() {Value = 250, Disp = "250ms"},
                    new TimeRange() {Value = 500, Disp = "500ms"},
                    new TimeRange() {Value = 1000, Disp = "1s"},
                    new TimeRange() {Value = 2000, Disp = "2s"},
                    new TimeRange() {Value = 5000, Disp = "5s"},
                    new TimeRange() {Value = 10000, Disp = "10s"},
                });
                }
            }
            private static int _timeSpan;
            public static int TimeSpan
            {
                get { return _timeSpan; }
                set
                {
                    if (_timeSpan == value) return;
                    _timeSpan = value;
                    if (TimeSpanChanged != null) TimeSpanChanged(null, EventArgs.Empty);
                }
            }
            public static event EventHandler TimeSpanChanged;

            private static int _displayAddr = MainWindow.CurrentDeviceAddress;
            public static int DisplayAddr
            {
                get { return _displayAddr; }
                set
                {
                    if (_displayAddr == value) return;
                    _displayAddr = value;
                    //NotifyPropertyChange("DisplayAddr");
                    if (DisplayAddrChanged != null) DisplayAddrChanged(null, EventArgs.Empty);
                }
            }
            public static event EventHandler DisplayAddrChanged;

            public int GetDisplayAddr()
            {
                return DisplayAddr;
            }
            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChange(string p)
            {
                //throw new NotImplementedException();
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(p));
            }


            public DataPlotterViewModel()
            {
                //FPS = new FPS();

                // Initialise data binding

                VoltageRange = 512;
                TimeSpan = 1000;

                PlotHeight = 22.5f;
                update_Labels();

                //    base.Loaded += OnContentRendered;
                //}

                //private void OnContentRendered(object sender, RoutedEventArgs e)
                //{
                DrawAxis();

                TimePlotter = new LiveView(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate, (uint)TimeSpan, VoltageRange,
                    Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                    (MainWindow.IsMatching)
                    );
                // Logic
                Plotter2D.Renderer = TimePlotter;
                TimeSpanBox.SelectionChanged += TimeSpan_SelectionChanged;
                VoltageRangeBox.SelectionChanged += TimeSpan_SelectionChanged;
            }

            private void DrawAxis()
            {
                if (PlotterAxis.Children.Count != 0) PlotterAxis.Children.Clear();

                var dashes = new DoubleCollection { 2, 2 };

                // Draw Dotted Lines#
                for (int i = 1; i < 10; i++)    //Serve as voltage scales
                {
                    var splitter = new Line();

                    splitter.Stroke = Brushes.Gray;
                    splitter.X1 = 0;
                    splitter.X2 = PlotterAxis.ActualWidth;
                    splitter.Y1 = splitter.Y2 = i * PlotterAxis.ActualHeight / 32;
                    splitter.StrokeThickness = 1;
                    splitter.StrokeDashArray = dashes;
                    splitter.StrokeDashCap = PenLineCap.Round;
                    PlotterAxis.Children.Add(splitter);
                    RenderOptions.SetCachingHint(splitter, CachingHint.Cache);
                    RenderOptions.SetBitmapScalingMode(splitter, BitmapScalingMode.LowQuality);

                }
            }

            private void update_Labels()
            {
                _channelLabel.Clear();
                for (var i = 31; i >= 0; i--)
                {
                    //_channelLabel.Add(new ChannelLabel() { height = plotHeight-0.3f, channelNum = i });
                    _channelLabel.Add(new ChannelLabel() { Height = (float)Plotter2D.ActualHeight / 32, ChannelNum = i });
                }
                //ChannelLabel.ItemsSource = _channelLabel;
            }

            private void UpdateGraph_Click(object sender, RoutedEventArgs e)
            {

                //Tbase = int.Parse(timeBase.Text);
                //TZoom = float.Parse(timeZoom.Text);
                PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
                PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight - 22);
                PlotHeight = Convert.ToSingle(PlotterHeight / 32);
                Plotter2D.Render();
            }

            private void root_SizeChanged(object sender, SizeChangedEventArgs e)
            {
                PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
                PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight - 22);
                PlotHeight = Convert.ToSingle(PlotterHeight / 32);
                //plotter2D.Render();
                update_Labels();
                DrawAxis();
            }

            bool isMouseDown = false;
            float mouseOrigin = 0;

            private void plotter2D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount != 1)
                {
                    return;
                }

                isMouseDown = true;
                Mouse.OverrideCursor = Cursors.Hand;
                mouseOrigin = (float)e.GetPosition(Plotter2D).X;
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
                    if (Zoom < 62500 / PlotterWidth) //Don't zoom in even further when only 1000 points on the screen
                    {
                        Tbase = getSafePosition(Tbase + Convert.ToInt32(e.GetPosition(Plotter2D).X / (zoomScale * Zoom)));
                        Zoom *= zoomScale;
                    }
                }
                else
                {
                    // Zoom out
                    if (Zoom > PlotterWidth / EstRecordLength) //Don't zoom out even further when whole record is shown
                    {
                        Tbase = getSafePosition(Tbase - Convert.ToInt32(e.GetPosition(Plotter2D).X / (zoomScale * Zoom)));
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
                        Tbase = getSafePosition(Tbase + (mouseOrigin + -(float)(e.GetPosition(Plotter2D).X)) / Zoom);
                        //timeBase.Text = Tbase.ToString();
                        Plotter2D.Render();
                        mouseOrigin = (float)e.GetPosition(Plotter2D).X;
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

                if (dispTimeBase > EstRecordLength - PlotterWidth * 0.6f / Zoom)
                {
                    safePosition = EstRecordLength - PlotterWidth * 0.6f / Zoom;
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

            private void TimeSpan_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                //plotter2D.IsLoopRendering = false;
                //timePlotter.UpdateTimeSpan(timeSpan);
                //plotter2D.Renderer.Reset(plotter2D.GetDrawEventArgs());
                //plotter2D.IsLoopRendering = true;
                if (Plotter2D != null)
                {
                    Plotter2D.IsLoopRendering = false;
                    TimePlotter.Dispose();
                    Plotter2D.Renderer = null;
#if DEBUG_GPU
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
#endif
                    TimePlotter = new LiveView(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate, (uint)TimeSpan, VoltageRange,
                        Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                        (MainWindow.IsMatching)
                        );
                    Plotter2D.Renderer = TimePlotter;
                    Plotter2D.IsLoopRendering = true;
                }
                //test.Dispose();
                //plotter2D.Renderer = null;
                //test = new LiveView((double)MainWindow.INTAN.samplingRate, timeSpan, voltageRange, plotter2D.ActualWidth, plotter2D.ActualHeight);
                //plotter2D.Renderer = test;
                //plotter2D.UpdateLayout();
            }

    }
}
