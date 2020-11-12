using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ASIC_Interface;
using SharpDX.WPF;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for Realtime_Results.xaml
    /// </summary>


    public partial class RealtimeResults
    {
        public FPS FPS { 
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
        private ObservableCollection<ChannelLabel> _channelLabel;

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
            get { return _voltageRange;}
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


        public RealtimeResults()
        {
            //FPS = new FPS();

            InitializeComponent();

            // Initialise data binding
            
            VoltageRange = 512;
            TimeSpan = 1000;

            ////spikes = LoadSpikeData("D:\\TestLarge.nerv");
            //if (MainWindow.isMatching)
            //    spikes = LoadSpikeData("D:\\Stream_processed.nerv");
            //else
            //    rawsignal = LoadNeuralSignal("D:\\Stream_passthrough.nerv");

            //MainWindow.IsLiveViewRunning = true;

            //Tbase = int.Parse(timeBase.Text);
            //TZoom = float.Parse(timeZoom.Text);
            PlotHeight = 22.5f;
            update_Labels();

            //plotter2D.IsLoopRendering = true;
            //plotter2D.Renderer = new Scene3d10_2d1() { Renderer = new D3D10(), FPS = FPS };
            //while (plotter2D.ActualHeight == 0 || plotter2D.ActualHeight == 0) { UpdateLayout(); }

            #region Old display methods using D3
            //plotter2D.Renderer = new Scene3d10_2d1() { Renderer = new D2D1(), FPS = FPS, };
            //dxview11_2d.Renderer = new Scene_11_2D1() { Renderer = new D3D11_2D1() };

            //List<SpikeData> spikes = LoadSpikeData("D:\\TestDisplay.nerv");

            //UInt16[] time = new UInt16[spikes.Count];
            ////double[][] point = new double[128][];
            //double[] point = new double[spikes.Count];

            //Random r = new Random();

            //Color[] plotColour = new Color[128];
            //for (int i=0;i<128;i++)
            //{
            //    plotColour[i] = new HsbColor(2.8 * i, 0.9 + 0.1 * r.NextDouble(), 0.9 + 0.1 * r.NextDouble()).ToArgb();
            //}

            ////for (int i=0;i<128;i++)
            ////{
            ////    point[i] = new double[spikes.Count];
            ////}

            //for (int i=0; i<spikes.Count;i++)
            //{
            //    time[i] = spikes[i].time;
            //    //point[spikes[i].channelID*4+spikes[i].templateID][i] = spikes[i].channelID + (spikes[i].templateID + 1) * 0.2;
            //    point[i] = spikes[i].channelID + (spikes[i].templateID + 1) * 0.2;
            //}

            //var XDataSource = new EnumerableDataSource<UInt16>(time);
            //XDataSource.SetXMapping(x => x);

            ////for (int i=0; i<128; i++)
            ////{
            ////    var YDataSource = new EnumerableDataSource<double>(point[i]);
            ////    YDataSource.SetYMapping(y => y);
            ////    CompositeDataSource spikeDataSource = new CompositeDataSource(XDataSource, YDataSource);
            ////    DataDisplay.AddLineGraph(spikeDataSource, new Pen(Brushes.Red, 0), new CircleElementPointMarker { Size = 8, Fill = new SolidColorBrush(new HsbColor(2.8*i,0.9+0.1*r.NextDouble(),0.9+0.1*r.NextDouble()).ToArgb())}, new PenDescription("Spike"));
            ////}

            //var YDataSource = new EnumerableDataSource<double>(point);
            ////YDataSource.SetYMapping(y => y);
            //CompositeDataSource spikeDataSource = new CompositeDataSource(XDataSource, YDataSource);
            //DataDisplay.AddLineGraph(spikeDataSource, new Pen(Brushes.Red, 0), new CircleElementPointMarker { Size = 8, Fill = Brushes.Red }, new PenDescription("Spike"));

            //for (int i = 0; i < spikes.Count;i++ )
            //{
            //    ((LineGraph)DataDisplay.Children.ElementAt(14+i)).LinePen = new Pen(new SolidColorBrush(new HsbColor(2.8*i,0.9+0.1*r.NextDouble(),0.9+0.1*r.NextDouble()).ToArgb()), 1);
            //}

            //    DataDisplay.Legend.Visibility = Visibility.Hidden;

            //DataDisplay.Viewport.FitToView();
            #endregion
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            DrawAxis();

            TimePlotter = new LiveView(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate, (uint) TimeSpan, VoltageRange,
                Plotter2D.ActualWidth, Plotter2D.ActualHeight,
                (MainWindow.IsSpikeSorting)
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
            for (int i = 1; i < 32; i++)
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

        //private static List<SpikeData> LoadSpikeData(string spikeFile)
        //{
        //    List<SpikeData> results = new List<SpikeData>();
        //    FileStream fs = new FileStream(spikeFile, FileMode.Open);
        //    BinaryReader br = new BinaryReader(fs);
        //    UInt16 frameInterval = 32;
        //    UInt16 idleEnterTime = 0, idleExitTime = 0, idleFrameCount = 0;
        //    UInt16 currentFrame=0;
        //    int crossCount = 0;

        //    UInt16 templateID = 0;
        //    UInt16 channelID = 0, prevChannelID = 0, startChannelID=0, latestChannelID = 0;
        //    UInt16[] LastSpikeTime = new UInt16[32];
        //    UInt16 currSpikeTime = 0, markerSpikeTime = 0, latestSpikeTime = 0;
        //    bool idle = false, start = true, test = false, crossBoundray = false, crossChBoundary =false;

        //    var idx_inBlock = 0;
        //    uint[] inputBlockData = new uint[16];

        //    Int16 inputData = 0;

        //    // Skip the rubbish data from previous run
        //    br.ReadBytes(16 * 1024 * 12);
        //    // Skip the first repeating data produced by FX3
        //    br.ReadUInt32();

        //    while (br.BaseStream.Position != br.BaseStream.Length)
        //    {
        //        idx_inBlock = 0;
        //        // Read block of 16 samples
        //        // Exit when reaching the end or all 16 samples are read
        //        while ((br.BaseStream.Position != br.BaseStream.Length) && (idx_inBlock < 16))
        //        {
        //            // Read data
        //            inputBlockData[idx_inBlock] = br.ReadUInt32();
        //            idx_inBlock++;
        //        }

        //        if (idx_inBlock < 16)
        //        {
        //            // Cannot convert this imcomplete block into correct samples, discard and exit
        //            break;
        //        }
        //        else
        //        {
        //            // Transpose the results into 16 samples from 32 FPGAs and push into results
        //            for (byte i = 0; i < 1; i++)    // Check only one FPGA at address 0
        //            {
        //                // Loop for 32 addresses
        //                inputData = 0;
        //                for (byte j = 0; j < 16; j++)
        //                {
        //                    // Loop for 16 samples
        //                    // MSB first. Transpose anti-clock wise
        //                    inputData |= (Int16)((inputBlockData[j] & (1 << i)) << (15 - j));
        //                }

        //                //// Decide if it is valid
        //                if ((inputData & 0xC000) == 0xC000)
        //                {
        //                    //// Time Stamp counter overflows happened once
        //                    // if (idle)
        //                    // {
        //                    // 2ms frame marker
        //                    //lastFrame = currentFrame;
        //                    //currentFrame += frameInterval;
        //                    // }
        //                    // else
        //                    // {
        //                    crossCount++;
        //                    // }
        //                    //break;
        //                }
                         
        //                if ((inputData & spi_idle_pattern) == spi_idle_pattern)
        //                {

        //                    if (!idle)
        //                    {
        //                        // Received 8000, extract its timestamp as the lastest timestamp
        //                        idleEnterTime = Convert.ToUInt16(inputData & spike_time_mask);
        //                        idleExitTime = idleEnterTime;
        //                        idle = true;
        //                        idleFrameCount = 0;
        //                        crossBoundray = false;
        //                        start = false;
        //                    }
        //                    else
        //                    {
        //                        //idleCount++;
        //                        //if (idleCount % 32 == 0)
        //                        //    idleFrameCount++;
        //                        if (!crossBoundray)
        //                        {
        //                            if (Convert.ToUInt16(inputData & spike_time_mask) < idleExitTime)
        //                            {
        //                                if (Convert.ToUInt16(inputData & spike_time_mask) >= idleEnterTime)
        //                                {
        //                                    currentFrame += frameInterval;
        //                                    idleFrameCount++;
        //                                    start = true;
        //                                }
        //                                else
        //                                {
        //                                    crossBoundray = true;
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (Convert.ToUInt16(inputData & spike_time_mask) < idleExitTime)
        //                            {
        //                                currentFrame += frameInterval;
        //                                idleFrameCount++;
        //                                start = true;
        //                                crossBoundray = false;
        //                            }
        //                        }

        //                        idleExitTime = Convert.ToUInt16(inputData & spike_time_mask);

        //                        //if (!crossBoundray && (Convert.ToUInt16(inputData & spike_time_mask) < idleExitTime))
        //                        //{
        //                        //    // Haven't have cross boundary, check if it happens now.
        //                        //    crossBoundray = true;
        //                        //    idleExitTime = Convert.ToUInt16(inputData & spike_time_mask);
        //                        //}


        //                        //if (crossBoundray && 
        //                        //    (Convert.ToUInt16(inputData & spike_time_mask) >= idleEnterTime
        //                        //    || Convert.ToUInt16(inputData & spike_time_mask) < idleExitTime))
        //                        //{
        //                        //    currentFrame += frameInterval;
        //                        //    idleFrameCount++;
        //                        //    start = true;
        //                        //    crossBoundray = false;
        //                        //    idleExitTime = Convert.ToUInt16(inputData & spike_time_mask);
        //                        //}
        //                    }
        //                    break;
        //                }

        //                // Valid spikes

        //                //// Check if there is any idle frame received.
        //                //if (idleFrameCount != 0)
        //                //{
        //                //    // Adjust frame base
        //                //    currentFrame += (UInt16)(frameInterval * idleFrameCount);
        //                //    idleFrameCount = 0;
        //                //    start = true;
        //                //}

        //                idle = false;
                        

        //                // Extract Information for Template matching
        //                templateID = Convert.ToUInt16((inputData & spike_template_mask) >> 10);
        //                channelID = Convert.ToUInt16((inputData & spike_channel_mask) >> 5);
        //                currSpikeTime = Convert.ToUInt16(inputData & spike_time_mask);

        //                test = false;

        //                // Adjust received spike location
        //                if (start)
        //                {
        //                    // At least one counter full range away. Reset initial conditions
        //                    start = false;
                            
        //                    markerSpikeTime = currSpikeTime;
        //                    currSpikeTime += currentFrame;
                            
        //                    startChannelID = channelID;
        //                    prevChannelID = channelID;

        //                    crossChBoundary = false;
        //                }
        //                else
        //                {
        //                    // There are previous spike(s)

        //                    // Check if all channels has been visited by T.M.
        //                    if (!crossChBoundary)
        //                    {
        //                        if (channelID < prevChannelID)
        //                        {
        //                            if (channelID >= startChannelID)
        //                            {
        //                                startChannelID = channelID;
        //                                //if (latestSpikeTime - markerSpikeTime - currentFrame >= 32)
        //                                    //if (markerSpikeTime >= 0x0010 && ((UInt16)(latestSpikeTime % 32) <= (markerSpikeTime - 0x0010)))
        //                                    currentFrame += frameInterval;
        //                                //markerSpikeTime = (UInt16)(latestSpikeTime % 32);
        //                                    markerSpikeTime = currSpikeTime;
        //                            }
        //                            else
        //                            {
        //                                crossChBoundary = true;
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (channelID < prevChannelID)
        //                        {
        //                            startChannelID = channelID;
        //                            //if (markerSpikeTime >= 0x0010 && ((UInt16)(latestSpikeTime % 32) <= (markerSpikeTime - 0x0010)))
        //                                currentFrame += frameInterval;
        //                            //markerSpikeTime = (UInt16)(latestSpikeTime % 32);
        //                                markerSpikeTime = currSpikeTime;
        //                            crossBoundray = false;
        //                        }
        //                    }

        //                    prevChannelID = channelID;

        //                    if (markerSpikeTime <0x0010)
        //                    {
        //                        // The timestamp is on the LOWER half of the timestamp counter. 
        //                        //Incoming spikes (around the previous spike) potentially underflow to the previous frame
        //                        if (currSpikeTime > markerSpikeTime + 0x0010)
        //                        {
        //                            //prevSpikeTime = currSpikeTime;
        //                            //currentFrame -= frameInterval;
        //                            //currSpikeTime += currentFrame;
        //                            // Just plot but don't go back
        //                            if (currentFrame != 0)
        //                            {
        //                                currSpikeTime += (UInt16)(currentFrame - frameInterval);
        //                            }
        //                            test = true;
        //                        }
        //                        else
        //                        {
        //                            //prevSpikeTime = currSpikeTime;
        //                            currSpikeTime += currentFrame;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // The timestamp is on the UPPER half of the timestamp counter. 
        //                        //Incoming spikes (around the previous spike) potentially overflow to the next frame
        //                        if (currSpikeTime <= markerSpikeTime - 0x0010)
        //                        {
        //                            //prevSpikeTime = currSpikeTime;
        //                            //currentFrame += frameInterval;
        //                            currSpikeTime += (UInt16)(currentFrame + frameInterval);
        //                        }
        //                        else
        //                        {
        //                            //prevSpikeTime = currSpikeTime;
        //                            currSpikeTime += currentFrame;
        //                        }
        //                    }
        //                }

        //                // Check time sequence on individual channel. Probably won't hit this condition.
        //                if (currSpikeTime <= LastSpikeTime[channelID])
        //                {
        //                    // All the time tag should advanced one frameinterval
        //                    //prevSpikeTime += frameInterval;
        //                    //currentFrame += frameInterval;

        //                    //currSpikeTime += frameInterval;

        //                    while (currSpikeTime <= LastSpikeTime[channelID])
        //                    {
        //                        currSpikeTime += frameInterval;
        //                        currentFrame += frameInterval;
        //                    }
        //                    LastSpikeTime[channelID] = currSpikeTime;
        //                }
        //                else
        //                {
        //                    LastSpikeTime[channelID] = currSpikeTime;
        //                }

        //                // Update markers
        //                if (currSpikeTime >= latestSpikeTime)
        //                {
        //                    latestSpikeTime = currSpikeTime;
        //                    latestChannelID = channelID;
        //                }

        //                // Push results

        //                results.Add(new SpikeData(currSpikeTime, channelID, templateID));
        //                //latestTimestamp = Convert.ToUInt16(inputData & spike_time_mask);
        //                // Adjust spike time
        //                //if ((spikeTime + currentFrame - latestTimestamp) > 6)

                        
        //                //// Cannot reverse the time on the same channel!
        //                //if (!boundaryCrossed)
        //                //{
        //                //    if (curSpikeTime < spikeTime[channelID])
        //                //    {
        //                //        curSpikeTime += lastFrame;
        //                //    }
        //                //else if (curSpikeTime >= spikeTime[channelID])
        //                //{
        //                //    spikeTime[channelID] = curSpikeTime;
        //                //    curSpikeTime += currentFrame;
        //                //}
        //                //}
        //                //if (curSpikeTime < spikeTime[channelID] && !boundaryCrossed)
        //                //{
        //                //    curSpikeTime += lastFrame;
        //                //}
        //                //else if (curSpikeTime >= spikeTime[channelID])
        //                //{
        //                //    spikeTime[channelID] = curSpikeTime;
        //                //    curSpikeTime += currentFrame;
        //                //}
        //                //else if ()

        //                //// Two hard facts:
        //                //// 1. Timestamp carried by 0x8000 is the immediate timestamp. It is equivalent to the time at the entrance of template matching engine.
        //                //// 2. Each spike takes 2ms, so there will not be 2 spikes back-to-back that belongs to the previous frame
        //                //// Therefore, adujust the time when:
        //                //if (spikeTime <= idleEnterTime)    // Fact.1
        //                //{
        //                //    if (boundaryCrossed)
        //                //    {
        //                //        spikeTime += lastFrame;
        //                //    }
        //                //    else
        //                //        spikeTime += currentFrame;
        //                //}
        //                //else
        //                //{

        //                //}


        //                //if ((currSpikeTime + currentFrame - idleExitTime) < 1)
        //                //{
        //                //    // The timestamp is not continous, the spike belongs to the previous frame
        //                //    // This is for correctly identification of spikes 
        //                //    // that has not been completely compared when timestamp counter overflows.
        //                //    // We believe such spikes would not belongs to a 2ms frame that is more than 2ms back.
        //                //    currSpikeTime += lastFrame;
        //                //}
        //                //else
        //                //    currSpikeTime += currentFrame;

        //                //// Push the results into the List.
        //                //results.Add(new SpikeData(currSpikeTime, channelID, templateID));
        //            }
        //        }
        //    }

        //    estRecordLength = latestSpikeTime;

        //    br.Close();
        //    fs.Close();
        //    return results;
        //}

        //private static List<Int16>[,] LoadNeuralSignal(string neuralFile)
        //{
        //    List<Int16>[,] results = new List<Int16>[32, 32];


        //    FileStream fs = new FileStream(neuralFile, FileMode.Open);
        //    BinaryReader br = new BinaryReader(fs);

        //    byte channelID = 0;
        //    Int16 adc_output = 0;

        //    var idx_inBlock = 0;
        //    uint[] inputBlockData = new uint[16];

        //    Int16 inputData = 0;
        //    //while (br.BaseStream.Position != br.BaseStream.Length)

        //    // Skip the rubbish data from previous run
        //    br.ReadBytes(16 * 1024 * 12);
        //    // Skip the first repeating data produced by FX3
        //    br.ReadUInt32();

        //    while (br.BaseStream.Position < br.BaseStream.Length)
        //    {
        //        idx_inBlock = 0;

        //        // Read block of 16 samples
        //        // Exit when reaching the end or all 16 samples are read
        //        while ((br.BaseStream.Position < br.BaseStream.Length) && (idx_inBlock < 16))
        //        {
        //            // Read data
        //            inputBlockData[idx_inBlock] = br.ReadUInt32();  //If FPGA sending Little Endian
        //            //bytes = br.ReadBytes(4);
        //            //inputBlockData[idx_inBlock] = (uint)(((bytes[3] | (bytes[2] << 8)) | (bytes[1] << 0x10)) | (bytes[0] << 0x18));
        //            // Decide if it is valid
        //            //if ((inputBlockData[idx_inBlock] & 0x8000) == 1)
        //            //{
        //            //    // Received 8000, try the next one
        //            //    continue;
        //            //}
        //            //else
        //            {
        //                // Valid. Read another one
        //                idx_inBlock++;
        //            }
        //        }

        //        if (idx_inBlock < 16)
        //        {
        //            // Cannot convert this imcomplete block into correct samples, discard and exit
        //            break;
        //        }
        //        else
        //        {
        //            // Transpose the results into 16 samples from 32 FPGAs and push into results
        //            //for (byte i = 0; i < 32; i++)
        //            for (byte i = 0; i < 1; i++)    // Check only one FPGA at address 0
        //            {
        //                // Loop for 32 addresses
        //                inputData = 0;
        //                for (byte j = 0; j < 16; j++)
        //                {
        //                    // Loop for 16 samples
        //                    // MSB first. Transpose anti-clock wise
        //                    inputData |= (Int16)((inputBlockData[j] & (1 << i)) << (15 - j));
        //                }

        //                // Filter out blank data
        //                if ((inputData & 0x8000) == 0x8000)
        //                    break;

        //                // Extract Information for Template matching
        //                channelID = (byte)((inputData & signal_channel_mask) >> 9);
        //                adc_output = Convert.ToInt16(inputData & signal_value_mask);
                        
        //                // Push the results into the List.
        //                if (results[i,channelID] == null)
        //                {
        //                    results[i, channelID] = new List<Int16>();
        //                    results[i,channelID].Add(adc_output);
        //                }
        //                else
        //                {
        //                    results[i,channelID].Add(adc_output);
        //                    estRecordLength = results[i, channelID].Count;
        //                }
                        
        //            }
        //        }
        //    }

        //    br.Close();
        //    fs.Close();

        //    return results;
        //}

        private void update_Labels()
        {
            _channelLabel.Clear();
            for (var i = 31; i >=0; i--)
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
                if (Zoom < 62500/PlotterWidth) //Don't zoom in even further when only 1000 points on the screen
                {
                    Tbase = getSafePosition(Tbase + Convert.ToInt32(e.GetPosition(Plotter2D).X / (zoomScale * Zoom)));
                    Zoom *= zoomScale;
                }
            }
            else
            {
                // Zoom out
                if (Zoom > PlotterWidth/EstRecordLength) //Don't zoom out even further when whole record is shown
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
                    Tbase = getSafePosition(Tbase + (mouseOrigin + -(float)(e.GetPosition(Plotter2D).X))/Zoom);
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

        private float getSafePosition (float dispTimeBase)
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

        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    e.Cancel = true;  // cancels the window close
        //    //plotter2D.IsLoopRendering = false;
        //    this.Hide();      // Programmatically hides the window
        //}


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
                    (MainWindow.IsSpikeSorting)
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


    //public class SpikeData
    //{
    //    public UInt16 time;
    //    public UInt16 channelID;
    //    public UInt16 templateID;

    //    public SpikeData(UInt16 time, UInt16 channelID, UInt16 templateID)
    //    {
    //        this.time = time;
    //        this.channelID = channelID;
    //        this.templateID = templateID;
    //    }
    //}

    public class ChannelLabel : INotifyPropertyChanged
    {

        private float _height;
        private int _channelNum;
        public float Height
        {
            get { return _height; }
            set
            {
                if (Math.Abs(_height - value) < 0.001f) return;
                _height = value;
                NotifyPropertyChange("Height");
            }
        }
        public int ChannelNum
        {
            get { return _channelNum; }
            set
            {
                if (_channelNum == value) return;
                _channelNum = value;
                NotifyPropertyChange("ChannelNum");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        } 
    }

    //public class NeuralSignal
    //{
    //    //public byte address;
    //    //public byte channelID;
    //    public UInt16 adc_output;

    //    public NeuralSignal(UInt16 adc_output)
    //    {
    //        //this.address = address;
    //        //this.channelID = channelID;
    //        this.adc_output = adc_output;
    //    }
    //}
}
