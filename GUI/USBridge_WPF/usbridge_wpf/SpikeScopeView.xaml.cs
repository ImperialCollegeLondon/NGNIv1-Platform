using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using INTAN_RHD2000;
using NUnit.Framework;
using SharpDX.WPF;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for DataPlotter.xaml
    /// </summary>
    public partial class SpikeScopeView : INotifyPropertyChanged
    {
        public Task SpikeDetectTask;

        public FPS FPS { get; set; }

        public uint ChannelId { get; set; }
        public uint AddressId { get; set; }

        public static float Tbase { get; set; }
        public static float Zoom { get; set; }

        public static float PlotHeight { get; set; }

        public static float PlotterWidth { get; set; }

        public static float PlotterHeight { get; set; }

        public static int EstRecordLength = 0;

        //List<ChannelLabel> _channelLabel = new List<ChannelLabel>();

        public SpikeScope SpikePlotter;
        private ObservableCollection<ChannelLabel> _channelLabel = new ObservableCollection<ChannelLabel>();

        public static float[,][] SpikeBuffer = new float[32, 32][];
        private readonly Stopwatch _stopWatch = new Stopwatch();

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                NotifyPropertyChange("IsRunning");
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


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(p));
        }


        public SpikeScopeView(uint addressId, uint channelId)
        {
            //FPS = new FPS();
            AddressId = addressId;
            ChannelId = channelId;

            // Initialise data binding

            VoltageRange = (int) MainWindow.INTAN[addressId].FullRangeValue;
            PlotHeight = 22.5f;

            InitializeComponent();

            base.Loaded += OnContentRendered;
            // Non-Window doesn't have OnContentRenderMethod. Has to trace back to the base class event.

            SpikeDetectTask = Task.Factory.StartNew(() =>
            {
                SpikeDetect(MainWindow.INTAN[addressId].SamplingRate, addressId,
                    channelId);
            },
                TaskCreationOptions.LongRunning);

        }

        private void SpikePlot()
        {
            if (Plotter2D != null) Plotter2D.Render();
        }

        public void DrawConfigure()
        {
            if (DisplayConfigures != null)
            {
                DrawTemplates(DisplayConfigures);
                DrawThreshold(false);
            }
        }

        /// <summary>d
        /// Draws the thresholds.
        /// </summary>
        private void DrawThreshold(bool isUpdate)
        {
            var detectTh = new Line();

            var dashes = new DoubleCollection {2, 2};
            // Assuming the last item is always threshold.
            if (isUpdate && ConfigurePlotter.Children.Count != 0)
                ConfigurePlotter.Children.RemoveAt(ConfigurePlotter.Children.Count - 1);

            // Draw Threshold

            detectTh.Stroke = Brushes.Gray;
            detectTh.X1 = 0;
            detectTh.X2 = ConfigurePlotter.ActualWidth;
            detectTh.Y1 =
                detectTh.Y2 =
                    (1.0 - DisplayConfigures.DetectTh/MainWindow.INTAN[AddressId].FullRangeBit)*
                    ConfigurePlotter.ActualHeight;


            detectTh.StrokeThickness = 2;

            detectTh.StrokeDashArray = dashes;
            detectTh.StrokeDashCap = PenLineCap.Round;

            RenderOptions.SetCachingHint(detectTh, CachingHint.Cache);
            RenderOptions.SetBitmapScalingMode(detectTh, BitmapScalingMode.LowQuality);


            ConfigurePlotter.Children.Add(detectTh);
        }

        /// <summary>
        /// Draw templates for each channel
        /// </summary>
        private void DrawTemplates(NeuralChannel.Configuration configuration)
        {
            var path = new Path[4];
            var pathGeometry = new PathGeometry[4];
            var template = new PathFigure[4];
            var brush = new Brush[] {Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple};

            // Clear the canvas
            ConfigurePlotter.Children.Clear();
            ConfigurePlotter.Background = new SolidColorBrush(Colors.Transparent);

            for (int j = 0; j < 4; j++)
            {
                // Loop templates
                if (template[j] == null)
                {
                    template[j] = new PathFigure();
                }
                template[j].StartPoint = new Point(ConfigurePlotter.ActualWidth/4,
                    (1 - configuration.Templates[j].Samples[0]/MainWindow.INTAN[AddressId].FullRangeBit)*
                    ConfigurePlotter.ActualHeight);
                template[j].IsClosed = false;

                for (int k = 1; k < 16; k++)
                {
                    // Loop samples
                    template[j].Segments.Add(
                        new LineSegment(
                            new Point(ConfigurePlotter.ActualWidth/4 + (k*ConfigurePlotter.ActualWidth/15)/2,
                                (1 - configuration.Templates[j].Samples[k]/MainWindow.INTAN[AddressId].FullRangeBit)*
                                ConfigurePlotter.ActualHeight), true));
                }

                if (pathGeometry[j] == null)
                {
                    pathGeometry[j] = new PathGeometry();
                    path[j] = new Path();
                }
                pathGeometry[j].Figures.Add(template[j]);
                path[j] = new Path
                {
                    Data = pathGeometry[j],
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection {2, 2},
                    Stroke = brush[j]
                };

                RenderOptions.SetCachingHint(path[j], CachingHint.Cache);
                RenderOptions.SetBitmapScalingMode(path[j], BitmapScalingMode.LowQuality);


                ConfigurePlotter.Children.Add(path[j]);
            }
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            #region Spike Plot

            DrawAxis();
            LoadDispConfigure();
            DrawConfigure();
            SpikePlotter = new SpikeScope(MainWindow.INTAN[AddressId].SamplingRate,
                Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                (MainWindow.IsSpikeSorting), AddressId, ChannelId
                );
            // Logic
            Plotter2D.Renderer = SpikePlotter;

            #endregion
        }

        private void DrawAxis()
        {
            if (PlotterAxis.Children.Count != 0) PlotterAxis.Children.Clear();

            var dashes = new DoubleCollection {2, 2};
            int divisions = 10;
            // Draw Dotted Lines#
            for (int i = 1; i < divisions; i++) //Serve as voltage scales
            {
                var hsplitter = new Line();
                var vsplitter = new Line();

                hsplitter.Stroke = vsplitter.Stroke = Brushes.Gray;
                hsplitter.Opacity = vsplitter.Opacity = 0.5;
                hsplitter.X1 = 0;
                hsplitter.X2 = PlotterAxis.ActualWidth;
                hsplitter.Y1 = hsplitter.Y2 = i*PlotterAxis.ActualHeight/divisions;
                vsplitter.X1 = vsplitter.X2 = i*PlotterAxis.ActualWidth/divisions;
                vsplitter.Y2 = PlotterAxis.ActualHeight;
                vsplitter.Y1 = 0;
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

        private void UpdateGraph_Click(object sender, RoutedEventArgs e)
        {
            //Tbase = int.Parse(timeBase.Text);
            //TZoom = float.Parse(timeZoom.Text);
            PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
            PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight - 22);
            Plotter2D.Render();
        }

        private void ScopeSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                PlotterWidth = Convert.ToSingle(Plotter2D.ActualWidth);
                PlotterHeight = Convert.ToSingle(Plotter2D.ActualHeight);
                //plotter2D.Render();
                DrawAxis();
                //LoadDispConfigure();
                DrawConfigure();
            }
        }

        private bool isMouseDown = false;
        private float mouseOrigin = 0;
        private bool _isRunning;


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
            if (Plotter2D != null)
            {
                Plotter2D.IsLoopRendering = false;
                SpikePlotter.Dispose();
                Plotter2D.Renderer = null;
#if DEBUG_GPU
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
#endif
                //Todo Propagate address, channel parameter and make header to display

                SpikePlotter = new SpikeScope(MainWindow.INTAN[AddressId].SamplingRate,
                    Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                    (MainWindow.IsSpikeSorting), AddressId, ChannelId
                    );
                Plotter2D.Renderer = SpikePlotter;
                Plotter2D.IsLoopRendering = true;
            }
            //test.Dispose();
            //plotter2D.Renderer = null;
            //test = new LiveView((double)MainWindow.INTAN.samplingRate, timeSpan, voltageRange, plotter2D.ActualWidth, plotter2D.ActualHeight);
            //plotter2D.Renderer = test;
            //plotter2D.UpdateLayout();
        }

        private void SpikeDetect(double samplingRate, uint address, uint channelId)
        {
            int depth = 30, spikeNum = 0;
            var bufferDataLength = (int) (Math.Floor(2*samplingRate/1000));
                // Plot (-1ms, 1ms). Up to 30 superimposed spikes;

            var supraThresCnt = 0;
            var isDetected = false;

            SpikeBuffer[address, channelId] = new float[bufferDataLength*depth + 1];
                //one extra position for holding the most recent spike number
            int bufferWriteIdx = 0;

            for (var i = 0; i < SpikeBuffer[address, channelId].Length; i++)
            {
                SpikeBuffer[address, channelId][i] = -1;
            }

            while (IsRunning)
            {
                NeuralChannel.NeuralSignal datapoint;
                MainWindow.NeuralChannel[address, channelId].SpikeScopeFIFO.TryDequeue(out datapoint);
                var configuration = MainWindow.NeuralChannel[address, channelId].ChannelParameter;
                // If there is no data available. Sleep 1 ms
                if (datapoint == null)
                {
                    Thread.Sleep(2);
                    continue;
                }
                // Search for crossing event
                if (!isDetected &&
                    ((configuration.DetectTh >= 256 && datapoint.Amplitude > configuration.DetectTh) ||
                     (configuration.DetectTh < 256 && datapoint.Amplitude < configuration.DetectTh))
                    )
                {
                    supraThresCnt++;
                    if (supraThresCnt > 0) // 4 consecutive samples across the threshold will be marked for spike event
                    {
                        isDetected = true;
                        //if (_selectedChannel != null && channelId == _selectedChannel.SequenceId)
                        //{
                        //    //_beepMemoryStream.Seek(0, SeekOrigin.Begin);
                        //    //using (SoundPlayer SP = new SoundPlayer(_beepMemoryStream))
                        //    //{
                        //    //    SP.PlaySync();
                        //    //}
                        //    _beepSoundPlayer.PlaySync();
                        //}
                        bufferWriteIdx = bufferDataLength*spikeNum + bufferDataLength/2;
                            // The peak is in the middle of the buffer
                    }
                }
                else
                {
                    supraThresCnt = 0;
                }

                if (isDetected)
                {
                    SpikeBuffer[address, channelId][bufferWriteIdx++] = datapoint.Amplitude;

                    if (bufferWriteIdx == bufferDataLength*spikeNum + bufferDataLength) // Full buffer
                    {
                        SpikeBuffer[address, channelId][bufferDataLength*depth] = spikeNum;
                            // Update the most recent spike number

                        if (_stopWatch.IsRunning)
                        {
                            _stopWatch.Stop();
                            if (_stopWatch.ElapsedMilliseconds > 16) // render every 16ms, limit the frame rate
                            {
                                // Plot
                                Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => SpikePlot()));
                                _stopWatch.Restart();
                            }
                            else
                                _stopWatch.Start();
                        }
                        else
                        {
                            Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => SpikePlot()));
                            _stopWatch.Start();
                        }

                        //System.Threading.Thread.Sleep(16);  // To limit the frame rate

                        // Wait for the next spike
                        isDetected = false;
                        spikeNum = (spikeNum + 1)%depth;
                    }
                }
                else
                {
                    // move the points
                    for (int i = bufferDataLength*spikeNum; i < bufferDataLength*spikeNum + bufferDataLength/2; i++)
                    {
                        SpikeBuffer[address, channelId][i] = SpikeBuffer[address, channelId][i + 1];
                    }
                    // push the last value
                    SpikeBuffer[address, channelId][bufferDataLength*spikeNum + bufferDataLength/2 - 1] =
                        datapoint.Amplitude;
                }
            }
        }

        private void ConfigurePlotter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            //MessageBox.Show(tmp.Tag.ToString());

            if (canvas != null)
            {
                var t = (1 - e.GetPosition(canvas).Y/canvas.ActualHeight)*MainWindow.INTAN[AddressId].FullRangeBit;
                DisplayConfigures.DetectTh =
                    MainWindow.NeuralChannel[AddressId, ChannelId].ChannelParameter.DetectTh = (short) (Math.Floor(t));
                DrawThreshold(true);
            }
        }

        private NeuralChannel.Configuration _displayConfigures;

        public NeuralChannel.Configuration DisplayConfigures
        {
            get
            {
                //if (_displayConfigures == null)
                //    _displayConfigures = new NeuralChannel.Configuration("init", 0, 0, new[]
                //                {new NeuralChannel.Configuration.Template(){ MatchingTh=1, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                //                new NeuralChannel.Configuration.Template(){ MatchingTh=2, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                //                new NeuralChannel.Configuration.Template(){ MatchingTh=3, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                //                new NeuralChannel.Configuration.Template(){ MatchingTh=4, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}}}
                //                );

                return _displayConfigures;
            }
            set
            {
                if (value == _displayConfigures) return;
                _displayConfigures = value;
                NotifyPropertyChange("DisplayConfigures");
            }
        }

        public void LoadDispConfigure()
        {
            DisplayConfigures = MainWindow.NeuralChannel[AddressId, ChannelId].ChannelParameter;
            TM1.Content = DisplayConfigures.Templates[0].MatchingTh;
            TM2.Content = DisplayConfigures.Templates[1].MatchingTh;
            TM3.Content = DisplayConfigures.Templates[2].MatchingTh;
            TM4.Content = DisplayConfigures.Templates[3].MatchingTh;
        }

        private void DetectThControl_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                DrawThreshold(true);
            }
        }

        //public void Dispose()
        //{
        //    if (Plotter2D != null)
        //    {
        //        Plotter2D.IsLoopRendering = false;
        //        SpikePlotter.Dispose();
        //        Plotter2D.Renderer = null;
        //        Plotter2D = null;
        //    }
        //    MainWindow.NeuralChannel[AddressId, ChannelId].IsSpikeDetecting = false;
        //    IsRunning = false;
        //}
    }
}
