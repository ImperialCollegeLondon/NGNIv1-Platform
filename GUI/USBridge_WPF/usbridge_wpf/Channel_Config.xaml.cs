﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ASIC_Interface;
using Microsoft.Win32;
using SharpDX.WPF;
using Path = System.Windows.Shapes.Path;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for Channel_Config.xaml
    /// </summary>
    public partial class ChannelConfig : INotifyPropertyChanged
    {
        const double BEEP_AMPLITUDE = ((1000 * 32768) / 1000) - 1;  // Amplitude = 1000
        const double BEEP_DELTA_FT = 2 * Math.PI * 1000 / 44100.0;  // Frequency = 1000

        const int BEEP_SAMPLES = (int)(441 * 1 / 10); // Duration = 1
        const int BEEP_BYTES = BEEP_SAMPLES * 4;
        private static readonly int[] BeepHdr = new int[] { 0x46464952, 36 + BEEP_BYTES, 0x45564157, 0x20746D66, 16, 0x20001, 44100, 176400, 0x100004, 0x61746164, BEEP_BYTES };
        private static MemoryStream _beepMemoryStream = new MemoryStream(44 + BEEP_BYTES);
        private static SoundPlayer _beepSoundPlayer = new SoundPlayer(_beepMemoryStream);
        private static BinaryWriter _beepBinaryWriter = new BinaryWriter(_beepMemoryStream);

        private string _cfgFile;
        readonly Task[] _spikePlotThreads = new Task[32];
        private ObservableCollection<NeuralChannel.Configuration> _displayConfigures;

        public ObservableCollection<NeuralChannel.Configuration> DisplayConfigures
        {
            get
            {
                return _displayConfigures ??
                       (_displayConfigures = new ObservableCollection<NeuralChannel.Configuration>());
            }
            set { _displayConfigures = value;}
        }

        public bool IsSpikePlotting
        {
            get { return _isSpikePlotting; }
            set
            {
                if (value == _isSpikePlotting) return;
                _isSpikePlotting = value;
                NotifyPropertyChange("IsSpikePlotting");
            }
        }

        List<int> _intanCfg = new List<int>();
        byte[] _readback = new byte[0x1000];

        byte[] _cfgStream;

        public static float[,][] SpikeBuffer = new float[32, 32][];

        double _canvasHeight;
        double _canvasWidth;
        NeuralChannel.Configuration _selectedChannel;
        int _displayIdx;
        readonly Stopwatch _stopWatch = new Stopwatch();
        private bool _isSpikePlotting;

        public ChannelConfig()
        {
            InitializeComponent();
            
            //INTANSetting.DataContext = MainWindow.INTAN;
            //Binding binding = new Binding() { Path = new PropertyPath("calibration_EN"), Source = MainWindow.INTAN };
            //this.Calibration_EN.SetBinding(CheckBox.IsCheckedProperty, binding);
            for (int addr = 0; addr < 32; addr++)
            {
                if (MainWindow.INTAN[addr].Connected)
                    load_cfg_file(addr);
            }
            InitBeepSound();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            load_display();
            update_display();
            InitSpikePlot(32);
            //DeviceAddressBox.SelectionChanged += StreamDevice_SelectionChanged;
        }

        private void Btn_Browse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog
            {
                FileName = CfgFilePath.Text,
                DefaultExt = ".*",
                Filter = "NGNI config (.bin) | *.bin"
            };
            if (openFileDialog1.ShowDialog() != true) return;
            CfgFilePath.Text = openFileDialog1.FileName;
            _cfgFile = openFileDialog1.FileName;
        }

        private void Btn_SendAll_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.IsDemo)
            {
                MessageBox.Show("Demo mode: Configuration Send");
            }
            else
            {
                _cfgStream = generate_cfg_stream(MainWindow.CurrentDeviceAddress);
                MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, _cfgStream, _cfgStream.Length,
                    MainWindow.CurrentDeviceAddress);
                MessageBox.Show("Configuration Send");
            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //private void IntegerUpDown_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (e.Delta < 0)
        //    {
        //        goToCh.Value = ((goToCh.Value + 32) < 992) ? goToCh.Value + 32 : 992;
        //    }
        //    else
        //    {
        //        goToCh.Value = ((goToCh.Value - 32) > 0) ? goToCh.Value - 32 : 1;
        //    }
        //}

        //private void goToCh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    update_display();
        //}

        private void update_display(bool drawTrace = true)
        {
            //if (ChannelConfiguration != null)
            //{
            //displayConfigures.Clear();

            //for (int i = 0; i < 32; i++)
            //{
            //    if (MainWindow.neuralChannel[0, i] != null)
            //        displayConfigures.Add(MainWindow.neuralChannel[0, i].channelParameter);
            //}

            //ChannelConfiguration.ItemsSource = displayConfigures;

            // Update canvas size
            _canvasHeight = ChannelConfiguration.ActualHeight / 4 - 10 - 60;
            _canvasWidth = ChannelConfiguration.ActualWidth / 8 - 10;

            if (!drawTrace) return;
            DrawTempaltes();
            DrawThresholds(true);
            //}
        }

        /// <summary>
        /// Load_displays this instance.
        /// </summary>
        private void load_display()
        {
            _displayConfigures.Clear();

            for (var i = 0; i < 32; i++)
            {
                if (MainWindow.NeuralChannel[0, i] != null)
                    _displayConfigures.Add(MainWindow.NeuralChannel[0, i].ChannelParameter);
            }

            //ChannelConfiguration.ItemsSource = _displayConfigures;
        }


        /// <summary>d
        /// Draws the thresholds.
        /// </summary>
        private void DrawThresholds( bool initial, int selectIdx = -1)
        {
            var detectTh = new Line[32];

            var dashes = new DoubleCollection {2, 2};

            for (var idx = (selectIdx < 0) ? 0 : selectIdx;
                idx < ((selectIdx < 0) ? DisplayConfigures.Count() : selectIdx + 1);
                idx++)
            {
                // Get Canvas
                var myListBoxItem = (ListBoxItem)ChannelConfiguration.ItemContainerGenerator.ContainerFromIndex(idx);
                myListBoxItem.ApplyTemplate();
                var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
                myContentPresenter.ApplyTemplate();
                var myDataTemplate = myContentPresenter.ContentTemplate;
                var templateCanvas = (Canvas)myDataTemplate.FindName("templateCanvas", myContentPresenter);

                // Assuming the last item is always threshold.
                if (!initial) templateCanvas.Children.RemoveAt(templateCanvas.Children.Count - 1);

                // Draw Threshold
                if (detectTh[idx] == null)
                {
                    detectTh[idx] = new Line();
                }
                detectTh[idx].Stroke = Brushes.Gray;
                detectTh[idx].X1 = 0;
                detectTh[idx].X2 = _canvasWidth;
                detectTh[idx].Y1 = detectTh[idx].Y2 = (1.0 - DisplayConfigures[idx].DetectTh / 512.0f) * _canvasHeight;


                detectTh[idx].StrokeThickness = 2;

                detectTh[idx].StrokeDashArray = dashes;
                detectTh[idx].StrokeDashCap = PenLineCap.Round;

                RenderOptions.SetCachingHint(detectTh[idx], CachingHint.Cache);
                RenderOptions.SetBitmapScalingMode(detectTh[idx], BitmapScalingMode.LowQuality);


                templateCanvas.Children.Add(detectTh[idx]);
            }
        }

        /// <summary>
        /// Draw templates for each channel
        /// </summary>
        private void DrawTempaltes()
        {
            var path = new Path[32 * 4];
            var pathGeometry = new PathGeometry[32 * 4];
            var template = new PathFigure[32 * 4];
            var idx = 0;

            foreach (var dispCfg in DisplayConfigures)
            {
                // Get Canvas
                var myListBoxItem = (ListBoxItem)ChannelConfiguration.ItemContainerGenerator.ContainerFromIndex(idx);
                myListBoxItem.ApplyTemplate();
                var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
                myContentPresenter.ApplyTemplate();
                var myDataTemplate = myContentPresenter.ContentTemplate;
                var templateCanvas = (Canvas)myDataTemplate.FindName("templateCanvas", myContentPresenter);
                var brush = new Brush[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple };

                // Clear the canvas
                templateCanvas.Children.Clear();
                templateCanvas.Background = new SolidColorBrush(Colors.Transparent);

                if (Math.Abs(_canvasHeight) > 0)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Loop templates
                        if (template[j + idx * 4] == null)
                        {
                            template[j + idx * 4] = new PathFigure();
                        }
                        template[j + idx * 4].StartPoint = new Point(_canvasWidth/4, (1 - dispCfg.Templates[j].Samples[0] / 512.0f) * _canvasHeight);
                        template[j + idx * 4].IsClosed = false;

                        for (int k = 1; k < 16; k++)
                        {
                            // Loop samples
                            template[j + idx * 4].Segments.Add(new LineSegment(new Point(_canvasWidth/4+(k * _canvasWidth / 15)/2, (1 - dispCfg.Templates[j].Samples[k] / 512.0f) * _canvasHeight), true));
                        }

                        if (pathGeometry[j + idx * 4] == null)
                        {
                            pathGeometry[j + idx * 4] = new PathGeometry();
                            path[j + idx * 4] = new Path();
                        }
                        pathGeometry[j + idx * 4].Figures.Add(template[idx * 4 + j]);
                        path[j + idx*4] = new Path
                        {
                            Data = pathGeometry[j + idx*4],
                            StrokeThickness = 3,
                            Stroke = brush[j]
                        };

                        RenderOptions.SetCachingHint(path[j + idx * 4], CachingHint.Cache);
                        RenderOptions.SetBitmapScalingMode(path[j + idx * 4], BitmapScalingMode.LowQuality);


                        templateCanvas.Children.Add(path[j + idx * 4]);
                    }
                }
                idx++;
            }
        }

        private void Btn_Load_Click(object sender, RoutedEventArgs e)
        {
            if (!load_cfg_file(MainWindow.CurrentDeviceAddress, CfgFilePath.Text)) return;
            if (IsSpikePlotting)
            {
                StopSpikePlot();
                DeinitSpikePlot();
                load_display();
                update_display();
                InitSpikePlot(32);
                StartSpikePlot();
            }
            else
            {
                DeinitSpikePlot();
                load_display();
                update_display();
                InitSpikePlot(32);
            }
        }

        //private void ChNav_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    //goToCh.Value = (int)ChNav.Value;
        //    update_display();
        //}


        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new SaveFileDialog
            {
                FileName = CfgFilePath.Text,
                DefaultExt = ".*",
                Filter = "NGNI config (.*) | *.*"
            };
            if (saveFileDialog1.ShowDialog() != true) return;
            CfgFilePath.Text = saveFileDialog1.FileName;
            _cfgFile = saveFileDialog1.FileName;

            var fs = new FileStream(_cfgFile, FileMode.Create);
            _cfgStream = generate_cfg_stream(MainWindow.CurrentDeviceAddress);
            fs.Write(_cfgStream, 0, _cfgStream.Length);
            fs.Close();

            MessageBox.Show("Configuration saved");
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            update_display();
        }


        private void CFG_INTAN_Click(object sender, RoutedEventArgs e)
        {

            if (MainWindow.INTAN[MainWindow.CurrentDeviceAddress].CalibrationEn)
            {
                MainWindow.INTAN[MainWindow.CurrentDeviceAddress].Configuration.CreateCommandListRegisterConfig(ref _intanCfg, true);
                MainWindow.INTAN[MainWindow.CurrentDeviceAddress].CalibrationEn = false;
            }
            else
            {
                MainWindow.INTAN[MainWindow.CurrentDeviceAddress].Configuration.CreateCommandListRegisterConfig(ref _intanCfg, false);
            }

            var prevStatus = MainWindow.USBridge.DeviceStatus;
            MainWindow.USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Pass_To_ASIC, MainWindow.CurrentDeviceAddress);
            MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.ASIC, _intanCfg.SelectMany(i => BitConverter.GetBytes((ushort)(i & 0xFFFF))).ToArray(), 2 * _intanCfg.Count, MainWindow.CurrentDeviceAddress);
            MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, MainWindow.CurrentDeviceAddress);
            MainWindow.USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, prevStatus, MainWindow.CurrentDeviceAddress);

            //Binding binding = new Binding() { Path = new PropertyPath("calibration_EN"), Source = MainWindow.INTAN };
            //this.Calibration_EN.SetBinding(CheckBox.IsCheckedProperty, binding);


            /* Printing Results */
            var str = new StringBuilder();

            MainWindow.USBridge.cfg_receive(FX3_IGLOO_Nano.Target.ASIC, ref _readback, 2 * _intanCfg.Count, MainWindow.CurrentDeviceAddress);

            uint cmdIdx = 0;

            foreach (var cmd in _intanCfg)
            {
                str.AppendLine();
                str.AppendFormat("{0,0} {1:d}: 0x{2:X2}{3:X2} //", "Readback", cmdIdx, _readback[2 * cmdIdx + 1], _readback[2 * cmdIdx]);
                if (cmd < 0 || cmd > 0xffff)
                {
                    str.AppendFormat("  command[" + cmdIdx + "] = INVALID COMMAND: {0:X4}", cmd);
                }
                else if ((cmd & 0xc000) == 0x0000)
                {
                    var channel = (cmd & 0x3f00) >> 8;
                    str.AppendFormat("  command[" + cmdIdx + "] = CONVERT(" + channel + ")(CMD:{0:X4})", cmd);
                }
                else
                {
                    int reg;
                    switch ((cmd & 0xc000))
                    {
                        case 0xc000:
                            reg = (cmd & 0x3f00) >> 8;
                            str.AppendFormat("  command[" + cmdIdx + "] = READ(" + reg + ")(CMD:{0:X4})", cmd);
                            break;
                        case 0x8000:
                            reg = (cmd & 0x3f00) >> 8;
                            var data = (cmd & 0x00ff);
                            str.AppendFormat("  command[" + cmdIdx + "] = WRITE(" + reg + ", {0:X2})(CMD:{1:X4})", data, cmd);
                            break;
                        default:
                            switch (cmd)
                            {
                                case 0x5500:
                                    str.AppendFormat("  command[" + cmdIdx + "] = CALIBRATE({0:X4})", cmd);
                                    break;
                                case 0x6a00:
                                    str.AppendFormat("  command[" + cmdIdx + "] = CLEAR({0:X4})", cmd);
                                    break;
                                default:
                                    str.AppendFormat("  command[" + cmdIdx + "] = INVALID COMMAND: {0:X4}", cmd);
                                    break;
                            }
                            break;
                    }
                }
                cmdIdx++;
            }

            DebugDisplay.Text = str.ToString();

        }

        private void sampling_Rate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            const double sysClk = 403200000;
            var cmd = new byte[4];
            var sr = (int) Sampling_Rate.SelectedValue;

            var temp = sysClk / (sr * 32 * 16 * 2);
            var fx3ClkDiv = (ushort) (Math.Floor(temp));
            var isHalf = (byte) (temp - fx3ClkDiv > 0.5 ? 1 : 0);

            cmd[0] = (byte) (FX3.FX3Cmd.Change_Pib_Freq);
            BitConverter.GetBytes(fx3ClkDiv).CopyTo(cmd, 1);
            cmd[2] = isHalf;

            if ((FX3_IGLOO_Nano.Status) MainWindow.USBridge.DeviceStatus != FX3_IGLOO_Nano.Status.Disconnected)
            {
                MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.FX3, cmd, cmd.Length, 0);
            }

            // Synchronise all the addresses
            for (int addr = 0; addr < 32; addr++)
            {
                if (MainWindow.INTAN[addr].Connected)
                    MainWindow.INTAN[addr].SamplingRate = sr;
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;  // cancels the window close
            
            if (IsSpikePlotting)
            {
                IsSpikePlotting = false;
                // This will trigger Btn_Plot_Spike_UnChecked, so no need to do StopSpikePlot();
            }

            Hide();
        }

        private void Btn_Plot_Spike_Checked(object sender, RoutedEventArgs e)
        {
            StartSpikePlot();
        }

        private void Btn_Plot_Spike_UnChecked(object sender, RoutedEventArgs e)
        {
            StopSpikePlot();
        }

        public void StopSpikePlot()
        {
            for (var i = 0; i < 32; i++)
            {
                var channelId = i;
                if (!_spikePlotThreads[channelId].IsCompleted) _spikePlotThreads[channelId].Wait();
            }

            Btn_Plot_Spike.Content = "Plot spike";
        }

        private void StartSpikePlot()
        {
            for (var i = 0; i < 32; i++)
            {
                var channelId = i;
                _spikePlotThreads[channelId] = Task.Factory.StartNew(() => { SpikeDetect(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate, MainWindow.CurrentDeviceAddress, channelId); },
                    TaskCreationOptions.LongRunning);
            }

            Btn_Plot_Spike.Content = "Stop";
        }

        private void SpikeDetect(double samplingRate, int streamId, int channelId)
        {
            int depth = 30, spikeNum = 0;
            var bufferDataLength = (int)(Math.Floor(2 * samplingRate / 1000)); // Plot (-1ms, 1ms). Up to 30 superimposed spikes;

            var supraThresCnt = 0;
            var isDetected = false;

            SpikeBuffer[streamId, channelId] = new float[bufferDataLength * depth + 1]; //one extra position for holding the most recent spike number
            int bufferWriteIdx = 0;

            for (var i = 0; i < SpikeBuffer[streamId, channelId].Length; i++)
            {
                SpikeBuffer[streamId, channelId][i] = -1;
            }

            while (IsSpikePlotting)
            {
                NeuralChannel.NeuralSignal datapoint;
                MainWindow.NeuralChannel[streamId, channelId].RawSignal.TryDequeue(out datapoint);
                // If there is no data available. Sleep 1 ms
                if (datapoint == null)
                {
                    Thread.Sleep(2);
                    continue;
                }
                // Search for crossing event
                if (!isDetected &&
                    ((DisplayConfigures[channelId].DetectTh >= 256 && datapoint.Amplitude > DisplayConfigures[channelId].DetectTh) ||
                    (DisplayConfigures[channelId].DetectTh < 256 && datapoint.Amplitude < DisplayConfigures[channelId].DetectTh))
                    )
                {
                    supraThresCnt++;
                    if (supraThresCnt > 0)  // 4 consecutive samples across the threshold will be marked for spike event
                    {
                        isDetected = true;
                        if (_selectedChannel != null && channelId == _selectedChannel.SequenceId)
                        {
                            //_beepMemoryStream.Seek(0, SeekOrigin.Begin);
                            //using (SoundPlayer SP = new SoundPlayer(_beepMemoryStream))
                            //{
                            //    SP.PlaySync();
                            //}
                            _beepSoundPlayer.PlaySync();
                        }
                        bufferWriteIdx = bufferDataLength * spikeNum + bufferDataLength / 2; // The peak is in the middle of the buffer
                    }
                }
                else
                {
                    supraThresCnt = 0;
                }

                if (isDetected)
                {
                    SpikeBuffer[streamId, channelId][bufferWriteIdx++] = datapoint.Amplitude;

                    if (bufferWriteIdx == bufferDataLength * spikeNum + bufferDataLength)   // Full buffer
                    {
                        SpikeBuffer[streamId, channelId][bufferDataLength * depth] = spikeNum;  // Update the most recent spike number

                        if (_stopWatch.IsRunning)
                        {
                            _stopWatch.Stop();
                            if (_stopWatch.ElapsedMilliseconds > 16)    // render every 16ms, limit the frame rate
                            {
                                // Plot
                                Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => SpikePlot(channelId)));
                                _stopWatch.Restart();
                            }
                            else
                                _stopWatch.Start();
                        }
                        else
                        {
                            Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => SpikePlot(channelId)));
                            _stopWatch.Start();
                        }

                        //System.Threading.Thread.Sleep(16);  // To limit the frame rate

                        // Wait for the next spike
                        isDetected = false;
                        spikeNum = (spikeNum + 1) % depth;
                    }
                }
                else
                {
                    // move the points
                    for (int i = bufferDataLength * spikeNum; i < bufferDataLength * spikeNum + bufferDataLength / 2; i++)
                    {
                        SpikeBuffer[streamId, channelId][i] = SpikeBuffer[streamId, channelId][i + 1];
                    }
                    // push the last value
                    SpikeBuffer[streamId, channelId][bufferDataLength * spikeNum + bufferDataLength / 2 - 1] = datapoint.Amplitude;
                }
            }
        }

        public static void InitBeepSound()
        {
            //double A = ((Amplitude * (System.Math.Pow(2, 15))) / 1000) - 1;
            //double DeltaFT = 2 * Math.PI * Frequency / 44100.0; 

            //int Samples = 441 * Duration / 10; 
            //int Bytes = Samples * 4;
            //int[] Hdr = { 0X46464952, 36 + Bytes, 0X45564157, 0X20746D66, 16, 0X20001, 44100, 176400, 0X100004, 0X61746164, Bytes };
            //using (MemoryStream MS = new MemoryStream(44 + Bytes))
            //{
            //using (var BW = new BinaryWriter(_beepMemoryStream))
            //    {
                    for (int I = 0; I < BeepHdr.Length; I++)
                    {
                        _beepBinaryWriter.Write(BeepHdr[I]);
                    }
                    for (int T = 0; T < BEEP_SAMPLES; T++)
                    {
                        short sample = System.Convert.ToInt16(BEEP_AMPLITUDE * Math.Sin(BEEP_DELTA_FT * T));
                        _beepBinaryWriter.Write(sample);
                        _beepBinaryWriter.Write(sample);
                    }
                    _beepBinaryWriter.Flush();
                    _beepMemoryStream.Seek(0, SeekOrigin.Begin);
                    //using (SoundPlayer SP = new SoundPlayer(MS))
                    //{
                    //    SP.PlaySync();
                    //}
                //}
            //}
        }

        private void InitSpikePlot(int channelCount)
        {
            try
            {
                for (var channelId = 0; channelId < channelCount; channelId++)
                {
                    // Get Canvas
                    var myListBoxItem =
                        (ListBoxItem) ChannelConfiguration.ItemContainerGenerator.ContainerFromIndex(channelId);
                    myListBoxItem.ApplyTemplate();
                    var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
                    myContentPresenter.ApplyTemplate();
                    var myDataTemplate = myContentPresenter.ContentTemplate;
                    var spikePlot = (DXElement) myDataTemplate.FindName("spikePlot", myContentPresenter);
                    if (spikePlot == null) throw new ArgumentNullException();

                    spikePlot.Renderer = new SpikePlot((int) _canvasWidth, (int) _canvasHeight,
                        MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate, channelId);
                }
            }
            catch
            {
                MessageBox.Show("Error loading the spike plot. Configuration file maybe corrupted!");
            }
        }

        private void DeinitSpikePlot()
        {
            for (var channelId = 0; channelId < 32; channelId++)
            {
                // Get Canvas
                var myListBoxItem = (ListBoxItem)ChannelConfiguration.ItemContainerGenerator.ContainerFromIndex(channelId);
                myListBoxItem.ApplyTemplate();
                var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
                myContentPresenter.ApplyTemplate();
                var myDataTemplate = myContentPresenter.ContentTemplate;
                var spikePlot = (DXElement)myDataTemplate.FindName("spikePlot", myContentPresenter);
                if (spikePlot == null) throw new ArgumentNullException();

                spikePlot.Renderer = null;
            }
        }

        private void SpikePlot(int channelId)
        {
            // Get Canvas
            var myListBoxItem = (ListBoxItem)ChannelConfiguration.ItemContainerGenerator.ContainerFromIndex(channelId);
            myListBoxItem.ApplyTemplate();
            var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
            myContentPresenter.ApplyTemplate();
            var myDataTemplate = myContentPresenter.ContentTemplate;
            var spikePlot = (DXElement)myDataTemplate.FindName("spikePlot", myContentPresenter);

            spikePlot.Render();
        }

        private void templateCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var tmp = sender as Canvas;
            //MessageBox.Show(tmp.Tag.ToString());

            if (tmp != null && (e.ClickCount != 1
                                ||
                                _selectedChannel == null
                                ||
                                _selectedChannel.SequenceId != (int)tmp.Tag))
            {
                return;
            }

            var t = (1 - e.GetPosition(tmp).Y / _canvasHeight) * 512f;
            //MainWindow.neuralChannel[(int)tmp.Tag / 32, (int)tmp.Tag % 32].channelParameter.DetectTh = (ushort)(Math.Floor(t));
            _selectedChannel.DetectTh = (short)(Math.Floor(t));
            DrawThresholds(false, _displayIdx);
        }

        private static TChildItem FindVisualChild<TChildItem>(DependencyObject obj)
            where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var item = child as TChildItem;
                if (item != null)
                    return item;
                else
                {
                    var childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void ChannelConfiguration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var myListBox = sender as ListBox;
            if (myListBox == null) return;
            _selectedChannel = myListBox.SelectedItem as NeuralChannel.Configuration;
            _displayIdx = myListBox.SelectedIndex;
        }

        /// <summary>
        /// Load configuration from file for the FGPA @ deviceAddr
        /// </summary>
        /// <param name="deviceAddr">The device addr.</param>
        /// <param name="cfgFile">The _CFG file.</param>
        public static bool load_cfg_file(int deviceAddr, string cfgFile = null)
        {
            var stage = FX3_IGLOO_Nano.CfgStage.Idle;
            Stream fs = null;
            BinaryReader br = null;

            ushort channelId = 0, templateId = 0, sampleId = 0;

            try
            {
                if (cfgFile == null)
                {
                    fs = new MemoryStream(Properties.Resources.defaultCfg);
                    br = new BinaryReader(fs);
                }
                else if (File.Exists(cfgFile))
                {
                    fs = new FileStream(cfgFile, FileMode.Open);
                    br = new BinaryReader(fs);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(@"The file does not exist!");
                    return false;
                }

                while (br != null && br.BaseStream.Position < br.BaseStream.Length)
                {
                    // When use big endian.
                    //var bytes = br.ReadBytes(2);
                    //uint cfgWord = (uint)(bytes[1] | (bytes[0] << 8));

                    // When use little endian.
                    uint cfgWord = br.ReadUInt16();

                    #region interpret command
                    if (stage == FX3_IGLOO_Nano.CfgStage.Idle)
                    {
                        switch (cfgWord & FX3_IGLOO_Nano.MASK_CMD)
                        {
                            case (ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Template:
                                // Skip the 16 zeros
                                br.ReadBytes(2);
                                stage = FX3_IGLOO_Nano.CfgStage.Set_Template;
                                //templateId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp.Template);
                                templateId = 0;
                                //channelId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp.Channel);
                                channelId = 0;
                                //sampleId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp.Channel);
                                sampleId = 0;
                                break;
                            case (ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Tmp_Thres:
                                // Skip the 16 zeros
                                br.ReadBytes(2);
                                stage = FX3_IGLOO_Nano.CfgStage.Set_Template_Thres;
                                //templateId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp_Th.Template);
                                templateId = 0;
                                //channelId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp_Th.Channel);
                                channelId = 0;
                                break;
                            case (ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Spd_Thres:
                                // Skip the 16 zeros
                                br.ReadBytes(2);
                                stage = FX3_IGLOO_Nano.CfgStage.Set_Detec_Thres;
                                //channelId = (ushort)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Spd_Th.Channel);
                                channelId = 0;
                                break;
                        }
                    }
                    #endregion
                    #region interpret data
                    else
                    {
                        if (cfgWord == (uint)FX3_IGLOO_Nano.IGLOO_CMDS.Stop)
                        {
                            // Stop command received?
                            stage = FX3_IGLOO_Nano.CfgStage.Idle;
                        }
                        else
                        {
                            // Process valid input

                            // Initialise configure if necessary
                            if (MainWindow.NeuralChannel[deviceAddr, channelId] == null)
                            {
                                // if not exist, create a new instance
                                MainWindow.NeuralChannel[deviceAddr, channelId] = new NeuralChannel("Ch. " + (deviceAddr * 32 + channelId).ToString(), deviceAddr * 32 + channelId);
                                // Open a file hanndel
                            }

                            switch (stage)
                            {
                                case FX3_IGLOO_Nano.CfgStage.Set_Template:
                                    MainWindow.NeuralChannel[deviceAddr, channelId].
                                        ChannelParameter.
                                        Templates[templateId].
                                            Samples[sampleId]
                                            = (short)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp.Value);
                                    // Update values
                                    if (sampleId < 15)
                                    {
                                        sampleId++;
                                    }
                                    else
                                    {
                                        sampleId = 0;
                                        if (channelId < 31)
                                        {
                                            channelId++;
                                        }
                                        else
                                        {
                                            channelId = 0;
                                            if (templateId < 3)
                                            {
                                                templateId++;
                                            }
                                            else
                                            {
                                                templateId = 0;
                                            }
                                        }
                                    }
                                    break;
                                case FX3_IGLOO_Nano.CfgStage.Set_Template_Thres:
                                    MainWindow.NeuralChannel[deviceAddr, channelId].
                                        ChannelParameter.
                                        Templates[templateId].MatchingTh = (short)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Tmp_Th.Value);
                                    // Update values
                                    if (channelId < 31)
                                    {
                                        channelId++;
                                    }
                                    else
                                    {
                                        channelId = 0;
                                        if (templateId < 3)
                                        {
                                            templateId++;
                                        }
                                        else
                                        {
                                            templateId = 0;
                                        }
                                    }
                                    break;
                                case FX3_IGLOO_Nano.CfgStage.Set_Detec_Thres:
                                    MainWindow.NeuralChannel[deviceAddr, channelId].
                                        ChannelParameter.DetectTh = (short)(cfgWord & (uint)FX3_IGLOO_Nano.Mask_Set_Spd_Th.Value);
                                    // Update values
                                    if (channelId < 31)
                                    {
                                        channelId++;
                                    }
                                    else
                                    {
                                        channelId = 0;
                                    }
                                    break;
                                case FX3_IGLOO_Nano.CfgStage.Idle:
                                    break;
                            }
                        }
                    }
                    #endregion
                }

            }
            catch
            {
                MainWindow.ShowMessage("Cannot load configuration file for device" + deviceAddr.ToString());
                return false;
            }
            finally
            {
                if (br != null) br.Close();
                if (fs != null) fs.Close();
            }
            return true;
        }

        /// <summary>
        /// Generate configuration stream for the FPGA @ deviceAddr
        /// </summary>
        /// <param name="deviceAddr"></param>
        /// <returns></returns>
        public static byte[] generate_cfg_stream(int deviceAddr)
        {
            List<byte> cfgStream = new List<byte>();
            List<byte> setDetectTh = new List<byte>();
            List<byte> setTemplates = new List<byte>();
            List<byte> setTemplatesTh = new List<byte>();

            for (int i = 0; i < 4; i++) // Loop templates
            {
                for (int j = 0; j < 32; j++) // Loop channels
                {
                    if (MainWindow.NeuralChannel[deviceAddr, j] != null)
                    {
                        // template threshold
                        setTemplatesTh.AddRange(BitConverter.GetBytes(MainWindow.NeuralChannel[deviceAddr, j].ChannelParameter.Templates[i].MatchingTh));
                        if (i == 0)
                        {
                            // detection threshold. Only need onece.
                            setDetectTh.AddRange(BitConverter.GetBytes(MainWindow.NeuralChannel[deviceAddr, j].ChannelParameter.DetectTh));
                        }
                        for (int k = 0; k < 16; k++) // Loop samples
                        {
                            // samples
                            setTemplates.AddRange(BitConverter.GetBytes(MainWindow.NeuralChannel[deviceAddr, j].ChannelParameter.Templates[i].Samples[k]));
                        }
                    }
                }
            }

            // Configure templates
            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Template)); // Start from channel 0, template 0, sample 0
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);
            cfgStream = cfgStream.Concat(setTemplates).ToList();

            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            // Configure template thresholds
            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Tmp_Thres)); // start from channel 0, template 0
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);
            cfgStream = cfgStream.Concat(setTemplatesTh).ToList();

            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            // Configure spike detection thresholds
            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Store_Spd_Thres)); // start from channel 0
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);
            cfgStream = cfgStream.Concat(setDetectTh).ToList();

            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            // Configure 16 to 9 ADC range mappping
            cfgStream.AddRange(
                BitConverter.GetBytes(
                    (ushort)
                        ((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Change_LSB |
                         (ushort)(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].LSBMapBit << 12) &
                         (ushort) (FX3_IGLOO_Nano.Mask_Set_LSB.LSBbit))));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            return cfgStream.ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void
            NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        private void Btn_SaveExit_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        //private void StreamDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //load_cfg_file(MainWindow.CurrentDeviceAddress, CfgFilePath.Text);
        //    if (IsSpikePlotting)
        //    {
        //        StopSpikePlot();
        //        DeinitSpikePlot();
        //        load_display();
        //        update_display();
        //        InitSpikePlot(MainWindow.CurrentDeviceAddress, 32);
        //        StartSpikePlot();
        //    }
        //    else
        //    {
        //        DeinitSpikePlot();
        //        load_display();
        //        update_display();
        //        InitSpikePlot(MainWindow.CurrentDeviceAddress, 32);
        //    }
        //}
    }
}
