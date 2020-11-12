using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using ASIC_Interface;
using INTAN_RHD2000;
using MahApps.Metro.Controls;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;


namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    
    public enum FileSaveModeList { AllRawData, PerChannel, PerAddress }

    public class ValueDispPair
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }

    //public class DeviceChannel
    //{
    //    public int Value { get; set; }
    //    public string Disp { get; set; }
    //}

    //public class LoggingMode
    //{
    //    public int Value { get; set; }
    //    public string Disp { get; set; }
    //}

    public partial class MainWindow : INotifyPropertyChanged
    {
        internal static MainWindow _appMainWindow;

        public static void SetWnd(MainWindow wnd)
        {
            _appMainWindow = wnd;
        }

        public static MainWindow AppMainWindow { get {return _appMainWindow;}}

        public delegate void SwitchModeHandler(object sender, SwitchModeEventArgs e);
        public event SwitchModeHandler OnSwitchMode;    

        public static IASIC_Interface USBridge, USBridge_Wifi;
        public static RHD2132_Igloo[] INTAN = new RHD2132_Igloo[32];
        public static NeuralChannel[,] NeuralChannel = new NeuralChannel[32, 32];
        Rhd2000Registers cfgSnapShot = new Rhd2000Registers(0, 0, 0);

        public int NumberOfDevices = 32;   // For USB HS, streaming 32 devices (1024 channels) might not be possible.
        public int ActiveDeviceNumber = 0;

        private static ulong _runningTime;

        #region Static Properties

        private static int _currentDeviceAddress;
        public static int CurrentDeviceAddress
        {
            get { return _currentDeviceAddress; }
            set
            {
                if (_currentDeviceAddress == value) return;
                _currentDeviceAddress = value;
                if (CurrentDeviceAddressChanged != null) CurrentDeviceAddressChanged(null, EventArgs.Empty);
            }
        }
        public static event EventHandler CurrentDeviceAddressChanged;

        private static ObservableCollection<ValueDispPair> _activeDeviceAddressList;
        public static ObservableCollection<ValueDispPair> ActiveDeviceAddressList
        {
            get
            {
                return _activeDeviceAddressList;
            }
            set
            {
                if (_activeDeviceAddressList == value) return;
                _activeDeviceAddressList = value;
                if (DeviceAddressListChanged != null) DeviceAddressListChanged(null, EventArgs.Empty);

            }
        }
        public static event EventHandler DeviceAddressListChanged;

        private static ObservableCollection<ValueDispPair> _activeChannelList;
        public static ObservableCollection<ValueDispPair> ActiveChannelList
        {
            get
            {
                return _activeChannelList ?? (_activeChannelList = new ObservableCollection<ValueDispPair>()
                {
                    new ValueDispPair() {Value = 33, Disp = "All Channels"},
                    new ValueDispPair() {Value = 0, Disp = "Channel 0"},
                    new ValueDispPair() {Value = 1, Disp = "Channel 1"},
                    new ValueDispPair() {Value = 2, Disp = "Channel 2"},
                    new ValueDispPair() {Value = 3, Disp = "Channel 3"},
                    new ValueDispPair() {Value = 4, Disp = "Channel 4"},
                    new ValueDispPair() {Value = 5, Disp = "Channel 5"},
                    new ValueDispPair() {Value = 6, Disp = "Channel 6"},
                    new ValueDispPair() {Value = 7, Disp = "Channel 7"},
                    new ValueDispPair() {Value = 8, Disp = "Channel 8"},
                    new ValueDispPair() {Value = 9, Disp = "Channel 9"},
                    new ValueDispPair() {Value = 10, Disp = "Channel 10"},
                    new ValueDispPair() {Value = 11, Disp = "Channel 11"},
                    new ValueDispPair() {Value = 12, Disp = "Channel 12"},
                    new ValueDispPair() {Value = 13, Disp = "Channel 13"},
                    new ValueDispPair() {Value = 14, Disp = "Channel 14"},
                    new ValueDispPair() {Value = 15, Disp = "Channel 15"},
                    new ValueDispPair() {Value = 16, Disp = "Channel 16"},
                    new ValueDispPair() {Value = 17, Disp = "Channel 17"},
                    new ValueDispPair() {Value = 18, Disp = "Channel 18"},
                    new ValueDispPair() {Value = 19, Disp = "Channel 19"},
                    new ValueDispPair() {Value = 20, Disp = "Channel 20"},
                    new ValueDispPair() {Value = 21, Disp = "Channel 21"},
                    new ValueDispPair() {Value = 22, Disp = "Channel 22"},
                    new ValueDispPair() {Value = 23, Disp = "Channel 23"},
                    new ValueDispPair() {Value = 24, Disp = "Channel 24"},
                    new ValueDispPair() {Value = 25, Disp = "Channel 25"},
                    new ValueDispPair() {Value = 26, Disp = "Channel 26"},
                    new ValueDispPair() {Value = 27, Disp = "Channel 27"},
                    new ValueDispPair() {Value = 28, Disp = "Channel 28"},
                    new ValueDispPair() {Value = 29, Disp = "Channel 29"},
                    new ValueDispPair() {Value = 30, Disp = "Channel 30"},
                    new ValueDispPair() {Value = 31, Disp = "Channel 31"}
                });

            }
            set
            {
                if (_activeChannelList == value) return;
                _activeChannelList = value;
                if (ActiveChannelListChanged != null) ActiveChannelListChanged(null, EventArgs.Empty);

            }
        }
        public static event EventHandler ActiveChannelListChanged;

        private static ObservableCollection<ValueDispPair> _recordingModeList;

        public static ObservableCollection<ValueDispPair> RecordingModeList
        {
            get
            {
                return _recordingModeList ?? (_recordingModeList = new ObservableCollection<ValueDispPair>()
                {
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Sig, Disp = "Neural Signal"},
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Spike, Disp = "Spike Events"},
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Sig | (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Spike, Disp = "Neural + Spike"}
                });
            }
        }

        private static ObservableCollection<ValueDispPair> _loggerModeList;

        public static ObservableCollection<ValueDispPair> LoggerModeList
        {
            get
            {
                return _loggerModeList ?? (_loggerModeList = new ObservableCollection<ValueDispPair>()
                {
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_Logger_Sig, Disp = "Neural Signal"},
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_Logger_Spike, Disp = "Spike Events"},
                    new ValueDispPair() {Value = (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_Logger_Sig | (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_Logger_Spike, Disp = "Neural + Spike"}
                });
            }
        }

        //private static bool _isSaveResults;

        private FileSaveModeList _fileSaveMode;

        public string ProjectFolder
        {
            get { return _projectFolder; }
            set
            {
                if (value == _projectFolder) return;
                _projectFolder = value;
                NotifyPropertyChange("ProjectFolder");
            }
        }

        //public static event EventHandler IsSaveResultsChanged;

        private bool _isSaveResults;
        public bool IsSaveResults
        {
            get { return _isSaveResults; }
            set
            {
                if (value == _isSaveResults) return;
                _isSaveResults = value;
                NotifyPropertyChange("IsSaveResults");
            }
        }

        private bool _isLogging;
        public bool IsLogging
        {
            get { return _isLogging; }
            set
            {
                if (value == _isLogging) return;
                _isLogging = value;
                NotifyPropertyChange("IsLogging");
            }
        }


        public static event EventHandler IsLiveViewRunningChanged;

        public static bool IsLiveViewRunning
        {
            get { return _isLiveViewRunning; }
            private set
            {
                if (value == _isLiveViewRunning) return;
                _isLiveViewRunning = value;
                if (IsLiveViewRunningChanged != null) IsLiveViewRunningChanged(null, EventArgs.Empty);
            }
        }

        public static event EventHandler IsStreamingChanged;

        public static bool IsStreaming
        {
            get { return _isStreaming; }
            private set
            {
                if (value == _isStreaming) return;
                _isStreaming = value;
                if (IsStreamingChanged != null) IsStreamingChanged(null, EventArgs.Empty);
            }
        }



        #endregion

        public FileSaveModeList FileSaveMode
        {
            get { return _fileSaveMode; }
            set
            {
                if (value == _fileSaveMode) return;
                _fileSaveMode = value;
                NotifyPropertyChange("FileSaveModeList");
                //NotifyPropertyChange("IsSaveResults");
            }
        }


        public static int FileSeqNo = 0;

        private Task _tListen;
        //CancellationTokenSource tokenSource = new CancellationTokenSource();

        public static event EventHandler IsSpikeSortingChanged;

        public static bool IsSpikeSorting
        {
            get
            {
                return _isSpikeSorting;
            }
            set
            {
                if (value == _isSpikeSorting) return;
                _isSpikeSorting = value;
                if (IsSpikeSortingChanged != null) IsSpikeSortingChanged(null, EventArgs.Empty);
            }
        }

        private ulong _writeDataLength;
        private ulong _invalidDataLength;
        public static ulong TimeBase;
        public static uint FifoSize = 1000;

        private readonly string[,] _perChannelResultFile = new string[32,32];
        private readonly string[,] _perChannelSpikeResultFile = new string[32, 32];
        private readonly string[] _perAddrResultFile = new string[32];
        private readonly string[] _perAddrSpikeResultFile = new string[32];

        private string _rawDataFile;
        public static FileStream[,] OutputWriter = new FileStream[32, 32];
        public static FileStream[,] OutputSpikeWriter = new FileStream[32, 32];
        private static readonly object[,] FileLock = new object[32,32];
        private static readonly Semaphore DataSavingLock = new Semaphore(1,1);
        private FileStream _rawDataStream;

        //Stream syncOutputfile;
        //BinaryWriter OutputWriter;
        private ActionBlock<byte[]> _writeRawData;
        private ActionBlock<NeuralChannel.RawData[]> _writeRestuls;
        private ActionBlock<NeuralChannel.RawData[]> _dataCasting;
        private byte[] _chopLeftOver;

        //private static ChannelConfig _cfgWindow;
        //private static RealtimeResults _resultsWindow;
        private bool _isDemo;
        public bool IsDemo
        {
            get { return _isDemo; }
            set
            {
                if (value == _isDemo) return;
                _isDemo = value;
                NotifyPropertyChange("IsDemo");
            }
        }

        private bool _isAutoStart;
        public bool IsAutoStart
        {
            get { return _isAutoStart; }
            set
            {
                if (value == _isAutoStart) return;
                _isAutoStart = value;
                NotifyPropertyChange("IsAutoStart");
            }
        }


        // These are needed to close the app from the Thread exception(exception handling)
        //delegate void ExceptionCallback();
        //ExceptionCallback handleException;

        //// These are  needed for Thread to update the UI
        //delegate void UpdateUICallback();
        //UpdateUICallback updateUI;

        private static readonly SyntheticSignal SyntheticSignal = new SyntheticSignal();
        private static bool _isLiveViewRunning;
        private static bool _isStreaming;
        private static bool _isSpikeSorting;
        private string _cfgFile;

        byte[] _cfgStream;
        private string _projectFolder;


        //private BackgroundWorker worker;
        //private DispatcherTimer SaveFileTimer;
        //private int saveFileInterval = 60000; //Milliseconds
        //DateTime lastSave;

        public MainWindow()
        {
            SetWnd(this);

            //this.DataContext=new BoundExampleModel(); // Not use if use Boot.cs
            //Timeline.DesiredFrameRateProperty.OverrideMetadata(
            //    typeof (Timeline),
            //    new FrameworkPropertyMetadata() {DefaultValue = 20});


            //SaveFileTimer = new DispatcherTimer(TimeSpan.FromMinutes(saveFileInterval), DispatcherPriority.Background,
            //    new EventHandler(DoAutoSave), this.Dispatcher);

            //SaveFileTimer.Stop();

            USBridge = new FX3_IGLOO_Nano(0x04B4, 0x00F0);
            USBridge_Wifi = new CC3200(5001, 9050, 3456);

            SearchDevice();

            // Setup the callback routine for NullReference exception handling
            //handleException = new ExceptionCallback(ThreadException);


            //usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);
            //usbDevices.DeviceAttached += new EventHandler(usbDevices_DeviceAttached);
            //usbDevices.DeviceRemoved += new EventHandler(usbDevices_DeviceRemoved);

            //if (usbDevices.Count > 0)
            //{
            //    // Get the first device having Vendor ID and ProductID
            //    USBridge = usbDevices[VID, PID] as CyUSBDevice;
            //    if (USBridge != null)
            //    {
            //        ctrlEpt = USBridge.ControlEndPt;
            //        InEpt = USBridge.BulkInEndPt;
            //        //IsoOutEpt = USBridge.IsocOutEndPt;
            //    }
            //}
            //IsSaveResults = false;

            InitializeComponent();
        }

        private void USBridge_Wifi_DeviceAttached(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            MessageBox.Show(@"Wifi Device attached on IP" + USBridge_Wifi.FriendlyName);
        }

        private void USBridge_Wifi_DeviceRemoved(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private UInt32 CurrentTimetoMCU_RTC()
        {
            
            return (UInt32)(DateTime.Now.Year - 1980) << 25
                   | (UInt32)(DateTime.Now.Month) << 21
                   | (UInt32)(DateTime.Now.Day) << 16
                   | (UInt32)(DateTime.Now.Hour) << 11
                   | (UInt32)(DateTime.Now.Minute) << 5
                   | (UInt32)(DateTime.Now.Second >> 1);
        }

        private void SearchDevice()
        {
            if ((FX3_IGLOO_Nano.Status)USBridge.DeviceStatus == FX3_IGLOO_Nano.Status.Disconnected) return;

            var templist = new ObservableCollection<ValueDispPair>();
            byte[] _readback = new byte[2];

            for (int addr = 0; addr < 32; addr++)
            {
                // Try find FPGA
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, addr);
                USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, addr);
                USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);

                if (_readback[0] == 0xCA && _readback[1] == 0x53)
                {
                    // Default INTAN, 15 kS/s, 1.0 - 10k bandpass
                    INTAN[addr] = new RHD2132_Igloo(15000, 300, 5000.0, 1)
                    {
                        Connected = true,
                        Enabled = true,
                        Configuration = { DSPCutoffFreq = 3 }
                    };
                    templist.Add(new ValueDispPair() { Value = addr, Disp = "Device " + addr });
                    CurrentDeviceAddress = addr;
                }
            }
            ActiveDeviceAddressList = templist;
            ActiveDeviceNumber = ActiveDeviceAddressList.Count;
        }

        private void ConfigAndStart()
        {
            if (ActiveDeviceNumber == 0) return;
            
            byte[] _readback = new byte[2];
#if V1
            // Try Stop the FPGA operation
            do
            {
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, CurrentDeviceAddress);
                USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
            } while (_readback[0] != 0xCA && _readback[1] != 0x53);


            // Try get the status of secondary link.
            USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Query_Status), 2, CurrentDeviceAddress);
            do
            {
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Null), 2, CurrentDeviceAddress);
                USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
            } while (_readback[0] == 0xCA && _readback[1] == 0x53);

            MessageBox.Show(String.Format("FPGA status: 0x{0:X2}{1:X2}", _readback[1], _readback[0]));

            if (_readback[1] == 0xFF)
            {
                INTAN[CurrentDeviceAddress].LSBMapBit = (_readback[0] & 0xE0) >> 5;
                MessageBox.Show(String.Format("LSB Bit: {0}, readback {1}", INTAN[CurrentDeviceAddress].LSBMapBit, (_readback[0] & 0xE0) >> 5));
                IsLogging = (_readback[0] & 0x10) == 0x10;

                Logger_RecMode.SelectedIndex = (_readback[0] & 0x3) == 0 ? 0 : (_readback[0] & 0x3) - 1;
                PC_RecMode.SelectedIndex = (_readback[0] & 0xC) >> 2 == 0 ? 0 : ((_readback[0] & 0xC) >> 2) - 1;

                if (!IsLogging)
                {
                    // Try reset
                    do
                    {
                        USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                            BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Soft_Reset), 2,
                            CurrentDeviceAddress);
                        USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
                    } while (_readback[0] != 0xCA && _readback[1] != 0x53);
                    MessageBox.Show("FPGA resetted!");
                }
                else
                {
                    ToggleButton_Logger.IsChecked = true;
                }
            }
            else
            {
                MessageBox.Show(String.Format("Error querying FPGA status: 0x{0}{1}", _readback[1].ToString(), _readback[0].ToString()));
                // Try reset
                do
                {
                    USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                        BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Soft_Reset), 2,
                        CurrentDeviceAddress);
                    USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
                } while (_readback[0] != 0xCA && _readback[1] != 0x53);
                MessageBox.Show("FPGA resetted!");
                Logger_RecMode.SelectedIndex = 0;
                PC_RecMode.SelectedIndex = 0;
            }


            // Try reset
            /* Commmented out because it will affect FGPA-MCU link */
            //do
            //{
            //    USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
            //        BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Soft_Reset), 2, CurrentDeviceAddress);
            //    USBridge.cfg_receive(FX3_IGLOO_Nano.Target.IGLOO, ref _readback, 2, CurrentDeviceAddress);
            //} while (_readback[0] != 0xCA && _readback[1] != 0x53);
            //MessageBox.Show("resetting");
#endif
            // Try updatge the RTC on Logger. 2 byte commands + 4 bytes timestamp
            USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Set_DeviceTime).Concat(BitConverter.GetBytes(CurrentTimetoMCU_RTC())).ToArray(), 6, CurrentDeviceAddress);

            // Put FPGA into desired mode
            SetRecMode();

            if (!IsDemo && !IsLogging) AmplifierSettings.UploadConfiguration();
            Thread.Sleep(100);
            Start_Streaming();
        }

        public uint NewAddressId { get; set; }
        public uint NewChannelId { get; set; }

        public static ulong RunningTime
        {
            get { return _runningTime; }
            set { _runningTime = value; }
        }

        static MainWindow()
        {
            IsLiveViewRunning = false;
        }

        //private void Save(object sender, ExecutedRoutedEventArgs e)
        //{
        //    if (worker != null && worker.IsBusy)
        //    {
        //        while (worker.IsBusy)
        //        {
        //            Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate() { }));
        //        }
        //    }

        //    if (worker != null)
        //        worker.DoWork += (s, dwe) =>
        //        {
        //            for (int i = 0; i < 32; i++)
        //            {
        //                for (int j = 0; j < 32; j++)
        //                {
        //                    if (NeuralChannel[i, j] != null)
        //                    {
        //                        NeuralChannel[i, j].CacheStream.Position = 0;
        //                        OutputStream[i * j].Position = 0;
        //                        OutputStream
        //                    }
        //                }
        //            }
        //        };
        //}

        //private void CanExecuteSave(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = this.IsLoaded && (worker == null || !worker.IsBusy);
        //}

        //private void DoAutoSave(object sender, EventArgs e)
        //{
        //    if ((worker == null || (worker != null && !worker.IsBusy)) &&
        //        DateTime.Now.Subtract(lastSave).TotalMilliseconds >= saveFileInterval
        //        && IsSaveResults && Btn_Stop.IsEnabled)
        //    {
        //        ApplicationCommands.Save.Execute(null, this);
        //    }
        //}

        /* 
         * Initial variables
         */

        private void Initialise()
        {
            //InEpt = null;
            //OutEpt = null;
            //USBridge = null;
            //_rawDataFile = "D:\\Stream.nerv";
            //BufSz = 0;
            //QueueSz = 0;
            //PPX = 0;
            //IsoPktBlockSize = 0;
            //IsSpikeSorting = false;
            
            // Always initialise the first 32 channels for demo or real device
            for (var i = 0; i < 32; i++)
            {
                NeuralChannel[0, i] = new NeuralChannel("Ch. " + i, i);
                NeuralChannel[1, i] = new NeuralChannel("Ch. " + i, i);

            }
        }

        //private void RefreshDeviceTree()
        //{
        //    DeviceTree.Items.Clear();
        //    //foreach (USBDevice dev in usbDevices)
        //    //{
        //    TreeViewItem treeItem = new TreeViewItem {Header = USBridge.FriendlyName};
        //    //if (USBridge.isConnected)
        //    //{
        //    DeviceTree.Items.Add(treeItem);
        //    //}
        //    //}
        //}

        private void RefreshControls()
        {
            if ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus == FX3_IGLOO_Nano.Status.Disconnected)
            {
                //Btn_Cfg.IsEnabled = true;
                //Btn_Rst.IsEnabled = true;
                //Btn_ModeToggle.IsEnabled = true;
                //Btn_ViewResults.IsEnabled = true;
                //Btn_Start.IsEnabled = true;
                //Btn_Stop.IsEnabled = false;
                DeviceStatus.Content = "Removed";
                DeviceMode.Content = "Demo!";
                IsDemo = true;
            }
            else
            {
                //Btn_Cfg.IsEnabled = true;
                //Btn_Rst.IsEnabled = true;
                //Btn_ModeToggle.IsEnabled = true;
                //Btn_ViewResults.IsEnabled = true;
                //Btn_Start.IsEnabled = true;
                //Btn_Stop.IsEnabled = false;
                DeviceStatus.Content = "Connected";
                DeviceMode.Content = "Configure";
                IsDemo = false;
            }

            //RefreshDeviceTree();
        }

        private void USBridge_DeviceAttached(object sender, EventArgs e)
        {
            if (!IsStreaming)
            {
                RefreshControls();
                SearchDevice();
                ConfigAndStart();
            }
            else
            {
                Stop_Streaming();
                RefreshControls();
                SearchDevice();
                ConfigAndStart();  // Restart in Demo mode
            }
        }

        private void USBridge_DeviceRemoved(object sender, EventArgs e)
        {
            if (IsStreaming)
            {
                Stop_Streaming();
                RefreshControls();
                SearchDevice();
                ConfigAndStart(); // Restart in Demo mode
            }
            else
            {
                RefreshControls();
            }
        }

        private void DeviceReset(object sender, RoutedEventArgs e)
        {
            //USBridge.ReConnect();
            USBridge.Reset();
        }

        //private void Btn_CFG_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_cfgWindow == null)
        //    {
        //        _cfgWindow = new ChannelConfig();
        //    }

        //    if (_cfgWindow.Visibility != Visibility.Visible)
        //        _cfgWindow.Show();
        //    else
        //    {
        //        _cfgWindow.Focus();
        //    }
        //}

        private void SetProjectFolder(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog {ShowNewFolderButton = true};
            if (folderBrowserDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            //// Open a save to file dialog to set Prefix
            //var saveFileDialog1 = new SaveFileDialog
            //{
            //    Title = @"Set the prefix for result files",
            //    InitialDirectory = folderBrowserDialog1.SelectedPath,
            //    Filter = @"Raw siganl device file (.raw) | *.raw | Neural Signal (.sig) | *.sig | Spike Event (.spike) | *.spike"
            //};

            //if (FileSaveMode == FileSaveModeList.AllRawData)
            //    saveFileDialog1.FilterIndex = 1;
            //else if (IsSpikeSorting)
            //    saveFileDialog1.FilterIndex = 3;
            //else
            //    saveFileDialog1.FilterIndex = 2;

            //if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            //FilePath.Text = saveFileDialog1.FileName;
            ProjectFolder = folderBrowserDialog1.SelectedPath;

            //UpdateSaveFilePath(FilePath.Text);
        }

        private void UpdateSaveFilePath(string directorypath, string filename)
        {
            // Get file path for data files
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    // Get all the names of output files.
                    _perChannelResultFile[i, j] = string.Format("{0}\\{1}_Addr_{2}_Ch_{3}{4}",
                        directorypath,
                        Path.GetFileNameWithoutExtension(filename),
                        i, j, ".sig");
                    _perChannelSpikeResultFile[i, j] = string.Format("{0}\\{1}_Addr_{2}_Ch_{3}{4}",
                        directorypath,
                        Path.GetFileNameWithoutExtension(filename),
                        i, j, ".spike");
                }

                _perAddrResultFile[i] = string.Format("{0}\\{1}_Addr_{2}{3}",
                        directorypath,
                        Path.GetFileNameWithoutExtension(filename),
                        i, ".sig");
                _perAddrSpikeResultFile[i] = string.Format("{0}\\{1}_Addr_{2}{3}",
                        directorypath,
                        Path.GetFileNameWithoutExtension(filename),
                        i, ".spike");
            }
            _rawDataFile = string.Format("{0}\\{1}.raw",
                directorypath,
                Path.GetFileNameWithoutExtension(filename));
        }

        //private void Btn_ViewResults_Click(object sender, RoutedEventArgs e)
        //{
        //    // Check if window is openning
        //    if (_resultsWindow != null)
        //    {
        //        _resultsWindow.Focus();
        //        return;
        //    }

        //    // Stop spike plot if it's running
        //    if (_cfgWindow != null && _cfgWindow.IsSpikePlotting)
        //    {
        //        _cfgWindow.IsSpikePlotting = false;
        //        _cfgWindow.StopSpikePlot();
        //    }

        //    // Show LiveView
        //    _resultsWindow = new RealtimeResults();
        //    if (IsSpikeSorting) 
        //        _resultsWindow.Title = _resultsWindow.Title + " - Spike Events";
        //    else
        //        _resultsWindow.Title = _resultsWindow.Title + " - Neural Signal";
        //    _resultsWindow.Closed += resultsWindow_Closed;
        //    _resultsWindow.Show();
        //    IsLiveViewRunning = true;
        //}

//        private static void resultsWindow_Closed(object sender, EventArgs e)
//        {
//            // Free all the resources used for live view
//            _resultsWindow.SignalPlotter.Dispose();
//            _resultsWindow.Plotter2D.Renderer = null;
//            _resultsWindow.Plotter2D = null;
//            _resultsWindow.Closed -= resultsWindow_Closed;
//            _resultsWindow = null;

//            IsLiveViewRunning = false;
//#if DEBUG_GPU
//            Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
//#endif
//        }

//        private void Btn_ModeToggle_Click(object sender, RoutedEventArgs e)
//        {
////#if MultiChannel
////                for (ushort addr = 0; addr < 32; addr++)
////                {
////                    // Toggle the mode for all the 32 devices
////                    USBridge[addr].toggleMode();
////                }
////#else
////            USBridge.toggleMode();
////#endif

//            if (IsLiveViewRunning)
//            {
//                var location = new Point(_resultsWindow.Left, _resultsWindow.Top);
//                _resultsWindow.Close();

//                if ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus == (FX3_IGLOO_Nano.Status.PC_sig))
//                {
//                    for (int addr = 0; addr < 32; addr++)
//                    {
//                        if (INTAN[addr].Connected)
//                            USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle_PC_spike, addr);
//                    }

//                    DeviceMode.Content = " MATCHING";
//                    IsSpikeSorting = true;
//                }
//                else if ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus == (FX3_IGLOO_Nano.Status.Idle_PC_spike))
//                {
//                    for (int addr = 0; addr < 32; addr++)
//                    {
//                        if (INTAN[addr].Connected)
//                            USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.PC_sig, addr);
//                    }

//                    DeviceMode.Content = " PASSTHROUGH";
//                    IsSpikeSorting = false;
//                }

//                _resultsWindow = new RealtimeResults();
//                if (IsSpikeSorting)
//                    _resultsWindow.Title = _resultsWindow.Title + " - Spike Events";
//                else
//                    _resultsWindow.Title = _resultsWindow.Title + " - Neural Signal";
//                _resultsWindow.Closed += resultsWindow_Closed;
//                _resultsWindow.Left = location.X;
//                _resultsWindow.Top = location.Y;
//                _resultsWindow.Show();
//                IsLiveViewRunning = true;
//            }
//            else
//            {
//                if ((FX3_IGLOO_Nano.Status)USBridge.DeviceStatus == (FX3_IGLOO_Nano.Status.PC_sig))
//                {
//                    for (int addr = 0; addr < 32; addr++)
//                    {
//                        if (INTAN[addr].Connected)
//                            USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle_PC_spike, addr);
//                    } DeviceMode.Content = " MATCHING";
//                    IsSpikeSorting = true;
//                }
//                else if ((FX3_IGLOO_Nano.Status)USBridge.DeviceStatus == (FX3_IGLOO_Nano.Status.Idle_PC_spike))
//                {
//                    for (int addr = 0; addr < 32; addr++)
//                    {
//                        if (INTAN[addr].Connected)
//                            USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.PC_sig, addr);
//                    } DeviceMode.Content = " PASSTHROUGH";
//                    IsSpikeSorting = false;
//                }
//            }
//        }


        /// <summary>
        /// Builds the pipeline for data processing.
        /// </summary>
        private void BuildPipeline()
        {
            /* Dataflow blocks */

#region Initial interface dataflow, hook up the entrance. 

            var buffer = new BufferBlock<byte[]>();

            if (IsDemo)
            {
                SyntheticSignal.InitInputDataFlow(buffer, 32);
            }
            else
            {
                USBridge.InitInputDataFlow(buffer, 32, 8);
                //USBridge.InitInputDataFlow(buffer, 2, 128);
                //USBridge.InitInputDataFlow(buffer, 32, 64);
            }

#endregion

#region Chop data into blocks with a size of number of devices by 16 (Transpose)

            var transpose = new TransformBlock<byte[], byte[]>(rawBytes =>
            {
                int originbyteIdx = 0;
                //System.Collections.Concurrent.ConcurrentQueue<byte> choppedData = new System.Collections.Concurrent.ConcurrentQueue<byte>();

                //int _tempData = 0;
                //var newbitIdx = 15;

                //Console.WriteLine("Number of bytes {0:d}", rawBytes.Length);

                if (_chopLeftOver != null)
                {
                    rawBytes = (_chopLeftOver.Concat(rawBytes)).ToArray();
                    MessageBox.Show("appended leftover bytes ");
                }

                // Start rotating
                /* original */
                //while (rawbyte_idx <= rawBytes.Length - 64)
                //{
                //    for (int channel_idx = 0; channel_idx < 32; channel_idx++)
                //    //Parallel.For(0, 32, (channel_idx) =>
                //    {
                //        for (int newbit_idx = 15; newbit_idx > 7; newbit_idx--)
                //        {
                //            _tempData = (_tempData << 1) | (rawBytes[rawbyte_idx + channel_idx / 8 + newbit_idx * 4] & (1 << (channel_idx % 8)));
                //        }
                //        choppedData.Add((byte)(_tempData));
                //        for (int newbit_idx = 7; newbit_idx >= 0; newbit_idx--)
                //        {
                //            _tempData = (_tempData << 1) | (rawBytes[rawbyte_idx + channel_idx / 8 + newbit_idx * 4] & (1 << (channel_idx % 8)));
                //        }
                //        choppedData.Add((byte)(_tempData));
                //    }
                //    rawbyte_idx += 64;
                //}

                /* better */

                //var choppedData = new List<byte>();
                //var tempData = new ushort[32];
                //var newbitIdx = 15;

                //for (originbyteIdx = 0; originbyteIdx < rawBytes.Length; originbyteIdx += 4)
                //// going through all the original data, process 4 bytes each time.
                //{
                //    for (var addressIdx = 0; addressIdx < 32; addressIdx++)
                //    // Accumulate new 16-bit word for each stream
                //    //Parallel.For(0, 32, channel_idx =>
                //    {
                //        tempData[addressIdx] =
                //            (ushort)
                //                ((tempData[addressIdx] << 1) |
                //                 ((rawBytes[originbyteIdx + addressIdx / 8] >> (addressIdx % 8)) & 0x1));
                //        // Starts from MSB to LSB
                //        if (newbitIdx != 0) continue;
                //        //if (tempData[addressIdx] != FX3_IGLOO_Nano.INVALID_DATA_MASK)
                //        //{
                //        // If data is valid.
                //        choppedData.Add(BitConverter.GetBytes(tempData[addressIdx])[0]); //Little Endian
                //        choppedData.Add(BitConverter.GetBytes(tempData[addressIdx])[1]);
                //        //}
                //        //else
                //        //    _invalidDataLength += 2;
                //    }
                //    newbitIdx = (newbitIdx == 0) ? 15 : (newbitIdx - 1);
                //}

                //if (originbyteIdx != rawBytes.Length)
                //{
                //    rawBytes.CopyTo(_chopLeftOver, originbyteIdx);
                //}
                //else
                //    _chopLeftOver = null;

                /* Unrolloed */

                var choppedData = new List<byte>();
                var tempData_64 = new byte[64];
                var tempData_16 = new byte[16];

                if (NumberOfDevices == 32)
                {

#region Unrolled loop for 32 by 16 blocks.

                    for (originbyteIdx = 0; originbyteIdx < rawBytes.Length; originbyteIdx += 64)
                    {
                        tempData_64[14] =
                            (byte) (((rawBytes[originbyteIdx + 32] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 36] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 40] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 44] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 48] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 52] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 56] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 60] & 0x80) >> 7));
                        tempData_64[15] =
                            (byte) (((rawBytes[originbyteIdx] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 4] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 8] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 12] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 16] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 20] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 24] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 28] & 0x80) >> 7));
                        tempData_64[12] =
                            (byte) (((rawBytes[originbyteIdx + 32] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 36] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 40] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 44] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 48] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 52] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 56] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 60] & 0x40) >> 6));
                        tempData_64[13] =
                            (byte) (((rawBytes[originbyteIdx] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 4] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 8] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 12] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 16] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 20] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 24] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 28] & 0x40) >> 6));
                        tempData_64[10] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x20) << 2) |
                                 ((rawBytes[originbyteIdx + 36] & 0x20) << 1) |
                                 ((rawBytes[originbyteIdx + 40] & 0x20)) |
                                 ((rawBytes[originbyteIdx + 44] & 0x20) >> 1) |
                                 ((rawBytes[originbyteIdx + 48] & 0x20) >> 2) |
                                 ((rawBytes[originbyteIdx + 52] & 0x20) >> 3) |
                                 ((rawBytes[originbyteIdx + 56] & 0x20) >> 4) |
                                 ((rawBytes[originbyteIdx + 60] & 0x20) >> 5));
                        tempData_64[11] =
                            (byte) (((rawBytes[originbyteIdx] & 0x20) << 2) |
                                    ((rawBytes[originbyteIdx + 4] & 0x20) << 1) |
                                    ((rawBytes[originbyteIdx + 8] & 0x20)) |
                                    ((rawBytes[originbyteIdx + 12] & 0x20) >> 1) |
                                    ((rawBytes[originbyteIdx + 16] & 0x20) >> 2) |
                                    ((rawBytes[originbyteIdx + 20] & 0x20) >> 3) |
                                    ((rawBytes[originbyteIdx + 24] & 0x20) >> 4) |
                                    ((rawBytes[originbyteIdx + 28] & 0x20) >> 5));
                        tempData_64[8] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x10) << 3) |
                                 ((rawBytes[originbyteIdx + 36] & 0x10) << 2) |
                                 ((rawBytes[originbyteIdx + 40] & 0x10) << 1) |
                                 ((rawBytes[originbyteIdx + 44] & 0x10)) |
                                 ((rawBytes[originbyteIdx + 48] & 0x10) >> 1) |
                                 ((rawBytes[originbyteIdx + 52] & 0x10) >> 2) |
                                 ((rawBytes[originbyteIdx + 56] & 0x10) >> 3) |
                                 ((rawBytes[originbyteIdx + 60] & 0x10) >> 4));
                        tempData_64[9] =
                            (byte) (((rawBytes[originbyteIdx] & 0x10) << 3) |
                                    ((rawBytes[originbyteIdx + 4] & 0x10) << 2) |
                                    ((rawBytes[originbyteIdx + 8] & 0x10) << 1) |
                                    ((rawBytes[originbyteIdx + 12] & 0x10)) |
                                    ((rawBytes[originbyteIdx + 16] & 0x10) >> 1) |
                                    ((rawBytes[originbyteIdx + 20] & 0x10) >> 2) |
                                    ((rawBytes[originbyteIdx + 24] & 0x10) >> 3) |
                                    ((rawBytes[originbyteIdx + 28] & 0x10) >> 4));
                        tempData_64[6] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x08) << 4) |
                                 ((rawBytes[originbyteIdx + 36] & 0x08) << 3) |
                                 ((rawBytes[originbyteIdx + 40] & 0x08) << 2) |
                                 ((rawBytes[originbyteIdx + 44] & 0x08) << 1) |
                                 ((rawBytes[originbyteIdx + 48] & 0x08)) |
                                 ((rawBytes[originbyteIdx + 52] & 0x08) >> 1) |
                                 ((rawBytes[originbyteIdx + 56] & 0x08) >> 2) |
                                 ((rawBytes[originbyteIdx + 60] & 0x08) >> 3));
                        tempData_64[7] =
                            (byte) (((rawBytes[originbyteIdx] & 0x08) << 4) |
                                    ((rawBytes[originbyteIdx + 4] & 0x08) << 3) |
                                    ((rawBytes[originbyteIdx + 8] & 0x08) << 2) |
                                    ((rawBytes[originbyteIdx + 12] & 0x08) << 1) |
                                    ((rawBytes[originbyteIdx + 16] & 0x08)) |
                                    ((rawBytes[originbyteIdx + 20] & 0x08) >> 1) |
                                    ((rawBytes[originbyteIdx + 24] & 0x08) >> 2) |
                                    ((rawBytes[originbyteIdx + 28] & 0x08) >> 3));
                        tempData_64[4] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x04) << 5) |
                                 ((rawBytes[originbyteIdx + 36] & 0x04) << 4) |
                                 ((rawBytes[originbyteIdx + 40] & 0x04) << 3) |
                                 ((rawBytes[originbyteIdx + 44] & 0x04) << 2) |
                                 ((rawBytes[originbyteIdx + 48] & 0x04) << 1) |
                                 ((rawBytes[originbyteIdx + 52] & 0x04)) |
                                 ((rawBytes[originbyteIdx + 56] & 0x04) >> 1) |
                                 ((rawBytes[originbyteIdx + 60] & 0x04) >> 2));
                        tempData_64[5] =
                            (byte) (((rawBytes[originbyteIdx] & 0x04) << 5) |
                                    ((rawBytes[originbyteIdx + 4] & 0x04) << 4) |
                                    ((rawBytes[originbyteIdx + 8] & 0x04) << 3) |
                                    ((rawBytes[originbyteIdx + 12] & 0x04) << 2) |
                                    ((rawBytes[originbyteIdx + 16] & 0x04) << 1) |
                                    ((rawBytes[originbyteIdx + 20] & 0x04)) |
                                    ((rawBytes[originbyteIdx + 24] & 0x04) >> 1) |
                                    ((rawBytes[originbyteIdx + 28] & 0x04) >> 2));
                        tempData_64[2] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x02) << 6) |
                                 ((rawBytes[originbyteIdx + 36] & 0x02) << 5) |
                                 ((rawBytes[originbyteIdx + 40] & 0x02) << 4) |
                                 ((rawBytes[originbyteIdx + 44] & 0x02) << 3) |
                                 ((rawBytes[originbyteIdx + 48] & 0x02) << 2) |
                                 ((rawBytes[originbyteIdx + 52] & 0x02) << 1) |
                                 ((rawBytes[originbyteIdx + 56] & 0x02)) |
                                 ((rawBytes[originbyteIdx + 60] & 0x02) >> 1));
                        tempData_64[3] =
                            (byte) (((rawBytes[originbyteIdx] & 0x02) << 6) |
                                    ((rawBytes[originbyteIdx + 4] & 0x02) << 5) |
                                    ((rawBytes[originbyteIdx + 8] & 0x02) << 4) |
                                    ((rawBytes[originbyteIdx + 12] & 0x02) << 3) |
                                    ((rawBytes[originbyteIdx + 16] & 0x02) << 2) |
                                    ((rawBytes[originbyteIdx + 20] & 0x02) << 1) |
                                    ((rawBytes[originbyteIdx + 24] & 0x02)) |
                                    ((rawBytes[originbyteIdx + 28] & 0x02) >> 1));
                        tempData_64[0] =
                            (byte)
                                (((rawBytes[originbyteIdx + 32] & 0x01) << 7) |
                                 ((rawBytes[originbyteIdx + 36] & 0x01) << 6) |
                                 ((rawBytes[originbyteIdx + 40] & 0x01) << 5) |
                                 ((rawBytes[originbyteIdx + 44] & 0x01) << 4) |
                                 ((rawBytes[originbyteIdx + 48] & 0x01) << 3) |
                                 ((rawBytes[originbyteIdx + 52] & 0x01) << 2) |
                                 ((rawBytes[originbyteIdx + 56] & 0x01) << 1) |
                                 ((rawBytes[originbyteIdx + 60] & 0x01)));
                        tempData_64[1] =
                            (byte) (((rawBytes[originbyteIdx] & 0x01) << 7) |
                                    ((rawBytes[originbyteIdx + 4] & 0x01) << 6) |
                                    ((rawBytes[originbyteIdx + 8] & 0x01) << 5) |
                                    ((rawBytes[originbyteIdx + 12] & 0x01) << 4) |
                                    ((rawBytes[originbyteIdx + 16] & 0x01) << 3) |
                                    ((rawBytes[originbyteIdx + 20] & 0x01) << 2) |
                                    ((rawBytes[originbyteIdx + 24] & 0x01) << 1) |
                                    ((rawBytes[originbyteIdx + 28] & 0x01)));
                        tempData_64[30] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x80)) |
                                 ((rawBytes[originbyteIdx + 37] & 0x80) >> 1) |
                                 ((rawBytes[originbyteIdx + 41] & 0x80) >> 2) |
                                 ((rawBytes[originbyteIdx + 45] & 0x80) >> 3) |
                                 ((rawBytes[originbyteIdx + 49] & 0x80) >> 4) |
                                 ((rawBytes[originbyteIdx + 53] & 0x80) >> 5) |
                                 ((rawBytes[originbyteIdx + 57] & 0x80) >> 6) |
                                 ((rawBytes[originbyteIdx + 61] & 0x80) >> 7));
                        tempData_64[31] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 5] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 9] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 13] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 17] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 21] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 25] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 29] & 0x80) >> 7));
                        tempData_64[28] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x40) << 1) |
                                 ((rawBytes[originbyteIdx + 37] & 0x40)) |
                                 ((rawBytes[originbyteIdx + 41] & 0x40) >> 1) |
                                 ((rawBytes[originbyteIdx + 45] & 0x40) >> 2) |
                                 ((rawBytes[originbyteIdx + 49] & 0x40) >> 3) |
                                 ((rawBytes[originbyteIdx + 53] & 0x40) >> 4) |
                                 ((rawBytes[originbyteIdx + 57] & 0x40) >> 5) |
                                 ((rawBytes[originbyteIdx + 61] & 0x40) >> 6));
                        tempData_64[29] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 5] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 9] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 13] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 17] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 21] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 25] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 29] & 0x40) >> 6));
                        tempData_64[26] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x20) << 2) |
                                 ((rawBytes[originbyteIdx + 37] & 0x20) << 1) |
                                 ((rawBytes[originbyteIdx + 41] & 0x20)) |
                                 ((rawBytes[originbyteIdx + 45] & 0x20) >> 1) |
                                 ((rawBytes[originbyteIdx + 49] & 0x20) >> 2) |
                                 ((rawBytes[originbyteIdx + 53] & 0x20) >> 3) |
                                 ((rawBytes[originbyteIdx + 57] & 0x20) >> 4) |
                                 ((rawBytes[originbyteIdx + 61] & 0x20) >> 5));
                        tempData_64[27] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x20) << 2) |
                                    ((rawBytes[originbyteIdx + 5] & 0x20) << 1) |
                                    ((rawBytes[originbyteIdx + 9] & 0x20)) |
                                    ((rawBytes[originbyteIdx + 13] & 0x20) >> 1) |
                                    ((rawBytes[originbyteIdx + 17] & 0x20) >> 2) |
                                    ((rawBytes[originbyteIdx + 21] & 0x20) >> 3) |
                                    ((rawBytes[originbyteIdx + 25] & 0x20) >> 4) |
                                    ((rawBytes[originbyteIdx + 29] & 0x20) >> 5));
                        tempData_64[24] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x10) << 3) |
                                 ((rawBytes[originbyteIdx + 37] & 0x10) << 2) |
                                 ((rawBytes[originbyteIdx + 41] & 0x10) << 1) |
                                 ((rawBytes[originbyteIdx + 45] & 0x10)) |
                                 ((rawBytes[originbyteIdx + 49] & 0x10) >> 1) |
                                 ((rawBytes[originbyteIdx + 53] & 0x10) >> 2) |
                                 ((rawBytes[originbyteIdx + 57] & 0x10) >> 3) |
                                 ((rawBytes[originbyteIdx + 61] & 0x10) >> 4));
                        tempData_64[25] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x10) << 3) |
                                    ((rawBytes[originbyteIdx + 5] & 0x10) << 2) |
                                    ((rawBytes[originbyteIdx + 9] & 0x10) << 1) |
                                    ((rawBytes[originbyteIdx + 13] & 0x10)) |
                                    ((rawBytes[originbyteIdx + 17] & 0x10) >> 1) |
                                    ((rawBytes[originbyteIdx + 21] & 0x10) >> 2) |
                                    ((rawBytes[originbyteIdx + 25] & 0x10) >> 3) |
                                    ((rawBytes[originbyteIdx + 29] & 0x10) >> 4));
                        tempData_64[22] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x08) << 4) |
                                 ((rawBytes[originbyteIdx + 37] & 0x08) << 3) |
                                 ((rawBytes[originbyteIdx + 41] & 0x08) << 2) |
                                 ((rawBytes[originbyteIdx + 45] & 0x08) << 1) |
                                 ((rawBytes[originbyteIdx + 49] & 0x08)) |
                                 ((rawBytes[originbyteIdx + 53] & 0x08) >> 1) |
                                 ((rawBytes[originbyteIdx + 57] & 0x08) >> 2) |
                                 ((rawBytes[originbyteIdx + 61] & 0x08) >> 3));
                        tempData_64[23] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x08) << 4) |
                                    ((rawBytes[originbyteIdx + 5] & 0x08) << 3) |
                                    ((rawBytes[originbyteIdx + 9] & 0x08) << 2) |
                                    ((rawBytes[originbyteIdx + 13] & 0x08) << 1) |
                                    ((rawBytes[originbyteIdx + 17] & 0x08)) |
                                    ((rawBytes[originbyteIdx + 21] & 0x08) >> 1) |
                                    ((rawBytes[originbyteIdx + 25] & 0x08) >> 2) |
                                    ((rawBytes[originbyteIdx + 29] & 0x08) >> 3));
                        tempData_64[20] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x04) << 5) |
                                 ((rawBytes[originbyteIdx + 37] & 0x04) << 4) |
                                 ((rawBytes[originbyteIdx + 41] & 0x04) << 3) |
                                 ((rawBytes[originbyteIdx + 45] & 0x04) << 2) |
                                 ((rawBytes[originbyteIdx + 49] & 0x04) << 1) |
                                 ((rawBytes[originbyteIdx + 53] & 0x04)) |
                                 ((rawBytes[originbyteIdx + 57] & 0x04) >> 1) |
                                 ((rawBytes[originbyteIdx + 61] & 0x04) >> 2));
                        tempData_64[21] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x04) << 5) |
                                    ((rawBytes[originbyteIdx + 5] & 0x04) << 4) |
                                    ((rawBytes[originbyteIdx + 9] & 0x04) << 3) |
                                    ((rawBytes[originbyteIdx + 13] & 0x04) << 2) |
                                    ((rawBytes[originbyteIdx + 17] & 0x04) << 1) |
                                    ((rawBytes[originbyteIdx + 21] & 0x04)) |
                                    ((rawBytes[originbyteIdx + 25] & 0x04) >> 1) |
                                    ((rawBytes[originbyteIdx + 29] & 0x04) >> 2));
                        tempData_64[18] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x02) << 6) |
                                 ((rawBytes[originbyteIdx + 37] & 0x02) << 5) |
                                 ((rawBytes[originbyteIdx + 41] & 0x02) << 4) |
                                 ((rawBytes[originbyteIdx + 45] & 0x02) << 3) |
                                 ((rawBytes[originbyteIdx + 49] & 0x02) << 2) |
                                 ((rawBytes[originbyteIdx + 53] & 0x02) << 1) |
                                 ((rawBytes[originbyteIdx + 57] & 0x02)) |
                                 ((rawBytes[originbyteIdx + 61] & 0x02) >> 1));
                        tempData_64[19] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x02) << 6) |
                                    ((rawBytes[originbyteIdx + 5] & 0x02) << 5) |
                                    ((rawBytes[originbyteIdx + 9] & 0x02) << 4) |
                                    ((rawBytes[originbyteIdx + 13] & 0x02) << 3) |
                                    ((rawBytes[originbyteIdx + 17] & 0x02) << 2) |
                                    ((rawBytes[originbyteIdx + 21] & 0x02) << 1) |
                                    ((rawBytes[originbyteIdx + 25] & 0x02)) |
                                    ((rawBytes[originbyteIdx + 29] & 0x02) >> 1));
                        tempData_64[16] =
                            (byte)
                                (((rawBytes[originbyteIdx + 33] & 0x01) << 7) |
                                 ((rawBytes[originbyteIdx + 37] & 0x01) << 6) |
                                 ((rawBytes[originbyteIdx + 41] & 0x01) << 5) |
                                 ((rawBytes[originbyteIdx + 45] & 0x01) << 4) |
                                 ((rawBytes[originbyteIdx + 49] & 0x01) << 3) |
                                 ((rawBytes[originbyteIdx + 53] & 0x01) << 2) |
                                 ((rawBytes[originbyteIdx + 57] & 0x01) << 1) |
                                 ((rawBytes[originbyteIdx + 61] & 0x01)));
                        tempData_64[17] =
                            (byte) (((rawBytes[originbyteIdx + 1] & 0x01) << 7) |
                                    ((rawBytes[originbyteIdx + 5] & 0x01) << 6) |
                                    ((rawBytes[originbyteIdx + 9] & 0x01) << 5) |
                                    ((rawBytes[originbyteIdx + 13] & 0x01) << 4) |
                                    ((rawBytes[originbyteIdx + 17] & 0x01) << 3) |
                                    ((rawBytes[originbyteIdx + 21] & 0x01) << 2) |
                                    ((rawBytes[originbyteIdx + 25] & 0x01) << 1) |
                                    ((rawBytes[originbyteIdx + 29] & 0x01)));
                        tempData_64[46] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x80)) |
                                 ((rawBytes[originbyteIdx + 38] & 0x80) >> 1) |
                                 ((rawBytes[originbyteIdx + 42] & 0x80) >> 2) |
                                 ((rawBytes[originbyteIdx + 46] & 0x80) >> 3) |
                                 ((rawBytes[originbyteIdx + 50] & 0x80) >> 4) |
                                 ((rawBytes[originbyteIdx + 54] & 0x80) >> 5) |
                                 ((rawBytes[originbyteIdx + 58] & 0x80) >> 6) |
                                 ((rawBytes[originbyteIdx + 62] & 0x80) >> 7));
                        tempData_64[47] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 6] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 10] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 14] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 18] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 22] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 26] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 30] & 0x80) >> 7));
                        tempData_64[44] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x40) << 1) |
                                 ((rawBytes[originbyteIdx + 38] & 0x40)) |
                                 ((rawBytes[originbyteIdx + 42] & 0x40) >> 1) |
                                 ((rawBytes[originbyteIdx + 46] & 0x40) >> 2) |
                                 ((rawBytes[originbyteIdx + 50] & 0x40) >> 3) |
                                 ((rawBytes[originbyteIdx + 54] & 0x40) >> 4) |
                                 ((rawBytes[originbyteIdx + 58] & 0x40) >> 5) |
                                 ((rawBytes[originbyteIdx + 62] & 0x40) >> 6));
                        tempData_64[45] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 6] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 10] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 14] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 18] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 22] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 26] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 30] & 0x40) >> 6));
                        tempData_64[42] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x20) << 2) |
                                 ((rawBytes[originbyteIdx + 38] & 0x20) << 1) |
                                 ((rawBytes[originbyteIdx + 42] & 0x20)) |
                                 ((rawBytes[originbyteIdx + 46] & 0x20) >> 1) |
                                 ((rawBytes[originbyteIdx + 50] & 0x20) >> 2) |
                                 ((rawBytes[originbyteIdx + 54] & 0x20) >> 3) |
                                 ((rawBytes[originbyteIdx + 58] & 0x20) >> 4) |
                                 ((rawBytes[originbyteIdx + 62] & 0x20) >> 5));
                        tempData_64[43] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x20) << 2) |
                                    ((rawBytes[originbyteIdx + 6] & 0x20) << 1) |
                                    ((rawBytes[originbyteIdx + 10] & 0x20)) |
                                    ((rawBytes[originbyteIdx + 14] & 0x20) >> 1) |
                                    ((rawBytes[originbyteIdx + 18] & 0x20) >> 2) |
                                    ((rawBytes[originbyteIdx + 22] & 0x20) >> 3) |
                                    ((rawBytes[originbyteIdx + 26] & 0x20) >> 4) |
                                    ((rawBytes[originbyteIdx + 30] & 0x20) >> 5));
                        tempData_64[40] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x10) << 3) |
                                 ((rawBytes[originbyteIdx + 38] & 0x10) << 2) |
                                 ((rawBytes[originbyteIdx + 42] & 0x10) << 1) |
                                 ((rawBytes[originbyteIdx + 46] & 0x10)) |
                                 ((rawBytes[originbyteIdx + 50] & 0x10) >> 1) |
                                 ((rawBytes[originbyteIdx + 54] & 0x10) >> 2) |
                                 ((rawBytes[originbyteIdx + 58] & 0x10) >> 3) |
                                 ((rawBytes[originbyteIdx + 62] & 0x10) >> 4));
                        tempData_64[41] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x10) << 3) |
                                    ((rawBytes[originbyteIdx + 6] & 0x10) << 2) |
                                    ((rawBytes[originbyteIdx + 10] & 0x10) << 1) |
                                    ((rawBytes[originbyteIdx + 14] & 0x10)) |
                                    ((rawBytes[originbyteIdx + 18] & 0x10) >> 1) |
                                    ((rawBytes[originbyteIdx + 22] & 0x10) >> 2) |
                                    ((rawBytes[originbyteIdx + 26] & 0x10) >> 3) |
                                    ((rawBytes[originbyteIdx + 30] & 0x10) >> 4));
                        tempData_64[38] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x08) << 4) |
                                 ((rawBytes[originbyteIdx + 38] & 0x08) << 3) |
                                 ((rawBytes[originbyteIdx + 42] & 0x08) << 2) |
                                 ((rawBytes[originbyteIdx + 46] & 0x08) << 1) |
                                 ((rawBytes[originbyteIdx + 50] & 0x08)) |
                                 ((rawBytes[originbyteIdx + 54] & 0x08) >> 1) |
                                 ((rawBytes[originbyteIdx + 58] & 0x08) >> 2) |
                                 ((rawBytes[originbyteIdx + 62] & 0x08) >> 3));
                        tempData_64[39] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x08) << 4) |
                                    ((rawBytes[originbyteIdx + 6] & 0x08) << 3) |
                                    ((rawBytes[originbyteIdx + 10] & 0x08) << 2) |
                                    ((rawBytes[originbyteIdx + 14] & 0x08) << 1) |
                                    ((rawBytes[originbyteIdx + 18] & 0x08)) |
                                    ((rawBytes[originbyteIdx + 22] & 0x08) >> 1) |
                                    ((rawBytes[originbyteIdx + 26] & 0x08) >> 2) |
                                    ((rawBytes[originbyteIdx + 30] & 0x08) >> 3));
                        tempData_64[36] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x04) << 5) |
                                 ((rawBytes[originbyteIdx + 38] & 0x04) << 4) |
                                 ((rawBytes[originbyteIdx + 42] & 0x04) << 3) |
                                 ((rawBytes[originbyteIdx + 46] & 0x04) << 2) |
                                 ((rawBytes[originbyteIdx + 50] & 0x04) << 1) |
                                 ((rawBytes[originbyteIdx + 54] & 0x04)) |
                                 ((rawBytes[originbyteIdx + 58] & 0x04) >> 1) |
                                 ((rawBytes[originbyteIdx + 62] & 0x04) >> 2));
                        tempData_64[37] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x04) << 5) |
                                    ((rawBytes[originbyteIdx + 6] & 0x04) << 4) |
                                    ((rawBytes[originbyteIdx + 10] & 0x04) << 3) |
                                    ((rawBytes[originbyteIdx + 14] & 0x04) << 2) |
                                    ((rawBytes[originbyteIdx + 18] & 0x04) << 1) |
                                    ((rawBytes[originbyteIdx + 22] & 0x04)) |
                                    ((rawBytes[originbyteIdx + 26] & 0x04) >> 1) |
                                    ((rawBytes[originbyteIdx + 30] & 0x04) >> 2));
                        tempData_64[34] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x02) << 6) |
                                 ((rawBytes[originbyteIdx + 38] & 0x02) << 5) |
                                 ((rawBytes[originbyteIdx + 42] & 0x02) << 4) |
                                 ((rawBytes[originbyteIdx + 46] & 0x02) << 3) |
                                 ((rawBytes[originbyteIdx + 50] & 0x02) << 2) |
                                 ((rawBytes[originbyteIdx + 54] & 0x02) << 1) |
                                 ((rawBytes[originbyteIdx + 58] & 0x02)) |
                                 ((rawBytes[originbyteIdx + 62] & 0x02) >> 1));
                        tempData_64[35] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x02) << 6) |
                                    ((rawBytes[originbyteIdx + 6] & 0x02) << 5) |
                                    ((rawBytes[originbyteIdx + 10] & 0x02) << 4) |
                                    ((rawBytes[originbyteIdx + 14] & 0x02) << 3) |
                                    ((rawBytes[originbyteIdx + 18] & 0x02) << 2) |
                                    ((rawBytes[originbyteIdx + 22] & 0x02) << 1) |
                                    ((rawBytes[originbyteIdx + 26] & 0x02)) |
                                    ((rawBytes[originbyteIdx + 30] & 0x02) >> 1));
                        tempData_64[32] =
                            (byte)
                                (((rawBytes[originbyteIdx + 34] & 0x01) << 7) |
                                 ((rawBytes[originbyteIdx + 38] & 0x01) << 6) |
                                 ((rawBytes[originbyteIdx + 42] & 0x01) << 5) |
                                 ((rawBytes[originbyteIdx + 46] & 0x01) << 4) |
                                 ((rawBytes[originbyteIdx + 50] & 0x01) << 3) |
                                 ((rawBytes[originbyteIdx + 54] & 0x01) << 2) |
                                 ((rawBytes[originbyteIdx + 58] & 0x01) << 1) |
                                 ((rawBytes[originbyteIdx + 62] & 0x01)));
                        tempData_64[33] =
                            (byte) (((rawBytes[originbyteIdx + 2] & 0x01) << 7) |
                                    ((rawBytes[originbyteIdx + 6] & 0x01) << 6) |
                                    ((rawBytes[originbyteIdx + 10] & 0x01) << 5) |
                                    ((rawBytes[originbyteIdx + 14] & 0x01) << 4) |
                                    ((rawBytes[originbyteIdx + 18] & 0x01) << 3) |
                                    ((rawBytes[originbyteIdx + 22] & 0x01) << 2) |
                                    ((rawBytes[originbyteIdx + 26] & 0x01) << 1) |
                                    ((rawBytes[originbyteIdx + 30] & 0x01)));
                        tempData_64[62] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x80)) |
                                 ((rawBytes[originbyteIdx + 39] & 0x80) >> 1) |
                                 ((rawBytes[originbyteIdx + 43] & 0x80) >> 2) |
                                 ((rawBytes[originbyteIdx + 47] & 0x80) >> 3) |
                                 ((rawBytes[originbyteIdx + 51] & 0x80) >> 4) |
                                 ((rawBytes[originbyteIdx + 55] & 0x80) >> 5) |
                                 ((rawBytes[originbyteIdx + 59] & 0x80) >> 6) |
                                 ((rawBytes[originbyteIdx + 63] & 0x80) >> 7));
                        tempData_64[63] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 7] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 11] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 15] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 19] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 23] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 27] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 31] & 0x80) >> 7));
                        tempData_64[60] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x40) << 1) |
                                 ((rawBytes[originbyteIdx + 39] & 0x40)) |
                                 ((rawBytes[originbyteIdx + 43] & 0x40) >> 1) |
                                 ((rawBytes[originbyteIdx + 47] & 0x40) >> 2) |
                                 ((rawBytes[originbyteIdx + 51] & 0x40) >> 3) |
                                 ((rawBytes[originbyteIdx + 55] & 0x40) >> 4) |
                                 ((rawBytes[originbyteIdx + 59] & 0x40) >> 5) |
                                 ((rawBytes[originbyteIdx + 63] & 0x40) >> 6));
                        tempData_64[61] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 7] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 11] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 15] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 19] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 23] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 27] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 31] & 0x40) >> 6));
                        tempData_64[58] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x20) << 2) |
                                 ((rawBytes[originbyteIdx + 39] & 0x20) << 1) |
                                 ((rawBytes[originbyteIdx + 43] & 0x20)) |
                                 ((rawBytes[originbyteIdx + 47] & 0x20) >> 1) |
                                 ((rawBytes[originbyteIdx + 51] & 0x20) >> 2) |
                                 ((rawBytes[originbyteIdx + 55] & 0x20) >> 3) |
                                 ((rawBytes[originbyteIdx + 59] & 0x20) >> 4) |
                                 ((rawBytes[originbyteIdx + 63] & 0x20) >> 5));
                        tempData_64[59] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x20) << 2) |
                                    ((rawBytes[originbyteIdx + 7] & 0x20) << 1) |
                                    ((rawBytes[originbyteIdx + 11] & 0x20)) |
                                    ((rawBytes[originbyteIdx + 15] & 0x20) >> 1) |
                                    ((rawBytes[originbyteIdx + 19] & 0x20) >> 2) |
                                    ((rawBytes[originbyteIdx + 23] & 0x20) >> 3) |
                                    ((rawBytes[originbyteIdx + 27] & 0x20) >> 4) |
                                    ((rawBytes[originbyteIdx + 31] & 0x20) >> 5));
                        tempData_64[56] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x10) << 3) |
                                 ((rawBytes[originbyteIdx + 39] & 0x10) << 2) |
                                 ((rawBytes[originbyteIdx + 43] & 0x10) << 1) |
                                 ((rawBytes[originbyteIdx + 47] & 0x10)) |
                                 ((rawBytes[originbyteIdx + 51] & 0x10) >> 1) |
                                 ((rawBytes[originbyteIdx + 55] & 0x10) >> 2) |
                                 ((rawBytes[originbyteIdx + 59] & 0x10) >> 3) |
                                 ((rawBytes[originbyteIdx + 63] & 0x10) >> 4));
                        tempData_64[57] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x10) << 3) |
                                    ((rawBytes[originbyteIdx + 7] & 0x10) << 2) |
                                    ((rawBytes[originbyteIdx + 11] & 0x10) << 1) |
                                    ((rawBytes[originbyteIdx + 15] & 0x10)) |
                                    ((rawBytes[originbyteIdx + 19] & 0x10) >> 1) |
                                    ((rawBytes[originbyteIdx + 23] & 0x10) >> 2) |
                                    ((rawBytes[originbyteIdx + 27] & 0x10) >> 3) |
                                    ((rawBytes[originbyteIdx + 31] & 0x10) >> 4));
                        tempData_64[54] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x08) << 4) |
                                 ((rawBytes[originbyteIdx + 39] & 0x08) << 3) |
                                 ((rawBytes[originbyteIdx + 43] & 0x08) << 2) |
                                 ((rawBytes[originbyteIdx + 47] & 0x08) << 1) |
                                 ((rawBytes[originbyteIdx + 51] & 0x08)) |
                                 ((rawBytes[originbyteIdx + 55] & 0x08) >> 1) |
                                 ((rawBytes[originbyteIdx + 59] & 0x08) >> 2) |
                                 ((rawBytes[originbyteIdx + 63] & 0x08) >> 3));
                        tempData_64[55] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x08) << 4) |
                                    ((rawBytes[originbyteIdx + 7] & 0x08) << 3) |
                                    ((rawBytes[originbyteIdx + 11] & 0x08) << 2) |
                                    ((rawBytes[originbyteIdx + 15] & 0x08) << 1) |
                                    ((rawBytes[originbyteIdx + 19] & 0x08)) |
                                    ((rawBytes[originbyteIdx + 23] & 0x08) >> 1) |
                                    ((rawBytes[originbyteIdx + 27] & 0x08) >> 2) |
                                    ((rawBytes[originbyteIdx + 31] & 0x08) >> 3));
                        tempData_64[52] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x04) << 5) |
                                 ((rawBytes[originbyteIdx + 39] & 0x04) << 4) |
                                 ((rawBytes[originbyteIdx + 43] & 0x04) << 3) |
                                 ((rawBytes[originbyteIdx + 47] & 0x04) << 2) |
                                 ((rawBytes[originbyteIdx + 51] & 0x04) << 1) |
                                 ((rawBytes[originbyteIdx + 55] & 0x04)) |
                                 ((rawBytes[originbyteIdx + 59] & 0x04) >> 1) |
                                 ((rawBytes[originbyteIdx + 63] & 0x04) >> 2));
                        tempData_64[53] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x04) << 5) |
                                    ((rawBytes[originbyteIdx + 7] & 0x04) << 4) |
                                    ((rawBytes[originbyteIdx + 11] & 0x04) << 3) |
                                    ((rawBytes[originbyteIdx + 15] & 0x04) << 2) |
                                    ((rawBytes[originbyteIdx + 19] & 0x04) << 1) |
                                    ((rawBytes[originbyteIdx + 23] & 0x04)) |
                                    ((rawBytes[originbyteIdx + 27] & 0x04) >> 1) |
                                    ((rawBytes[originbyteIdx + 31] & 0x04) >> 2));
                        tempData_64[50] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x02) << 6) |
                                 ((rawBytes[originbyteIdx + 39] & 0x02) << 5) |
                                 ((rawBytes[originbyteIdx + 43] & 0x02) << 4) |
                                 ((rawBytes[originbyteIdx + 47] & 0x02) << 3) |
                                 ((rawBytes[originbyteIdx + 51] & 0x02) << 2) |
                                 ((rawBytes[originbyteIdx + 55] & 0x02) << 1) |
                                 ((rawBytes[originbyteIdx + 59] & 0x02)) |
                                 ((rawBytes[originbyteIdx + 63] & 0x02) >> 1));
                        tempData_64[51] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x02) << 6) |
                                    ((rawBytes[originbyteIdx + 7] & 0x02) << 5) |
                                    ((rawBytes[originbyteIdx + 11] & 0x02) << 4) |
                                    ((rawBytes[originbyteIdx + 15] & 0x02) << 3) |
                                    ((rawBytes[originbyteIdx + 19] & 0x02) << 2) |
                                    ((rawBytes[originbyteIdx + 23] & 0x02) << 1) |
                                    ((rawBytes[originbyteIdx + 27] & 0x02)) |
                                    ((rawBytes[originbyteIdx + 31] & 0x02) >> 1));
                        tempData_64[48] =
                            (byte)
                                (((rawBytes[originbyteIdx + 35] & 0x01) << 7) |
                                 ((rawBytes[originbyteIdx + 39] & 0x01) << 6) |
                                 ((rawBytes[originbyteIdx + 43] & 0x01) << 5) |
                                 ((rawBytes[originbyteIdx + 47] & 0x01) << 4) |
                                 ((rawBytes[originbyteIdx + 51] & 0x01) << 3) |
                                 ((rawBytes[originbyteIdx + 55] & 0x01) << 2) |
                                 ((rawBytes[originbyteIdx + 59] & 0x01) << 1) |
                                 ((rawBytes[originbyteIdx + 63] & 0x01)));
                        tempData_64[49] =
                            (byte) (((rawBytes[originbyteIdx + 3] & 0x01) << 7) |
                                    ((rawBytes[originbyteIdx + 7] & 0x01) << 6) |
                                    ((rawBytes[originbyteIdx + 11] & 0x01) << 5) |
                                    ((rawBytes[originbyteIdx + 15] & 0x01) << 4) |
                                    ((rawBytes[originbyteIdx + 19] & 0x01) << 3) |
                                    ((rawBytes[originbyteIdx + 23] & 0x01) << 2) |
                                    ((rawBytes[originbyteIdx + 27] & 0x01) << 1) |
                                    ((rawBytes[originbyteIdx + 31] & 0x01)));

                        choppedData.AddRange(tempData_64);
                    }

#endregion

                }

                else if (NumberOfDevices == 8)
                {

#region Unrolled loop for 8 by 16 blocks.

                    for (originbyteIdx = 0; originbyteIdx < rawBytes.Length; originbyteIdx += 16)
                    {
                        tempData_16[14] =
                            (byte) (((rawBytes[originbyteIdx + 8] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 36] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 40] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 44] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 48] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 52] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 56] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 60] & 0x80) >> 7));
                        tempData_16[15] =
                            (byte) (((rawBytes[originbyteIdx] & 0x80)) |
                                    ((rawBytes[originbyteIdx + 1] & 0x80) >> 1) |
                                    ((rawBytes[originbyteIdx + 2] & 0x80) >> 2) |
                                    ((rawBytes[originbyteIdx + 3] & 0x80) >> 3) |
                                    ((rawBytes[originbyteIdx + 4] & 0x80) >> 4) |
                                    ((rawBytes[originbyteIdx + 5] & 0x80) >> 5) |
                                    ((rawBytes[originbyteIdx + 6] & 0x80) >> 6) |
                                    ((rawBytes[originbyteIdx + 7] & 0x80) >> 7));
                        tempData_16[12] =
                            (byte) (((rawBytes[originbyteIdx + 8] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 9] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 10] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 11] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 12] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 13] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 14] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 15] & 0x40) >> 6));
                        tempData_16[13] =
                            (byte) (((rawBytes[originbyteIdx] & 0x40) << 1) |
                                    ((rawBytes[originbyteIdx + 1] & 0x40)) |
                                    ((rawBytes[originbyteIdx + 2] & 0x40) >> 1) |
                                    ((rawBytes[originbyteIdx + 3] & 0x40) >> 2) |
                                    ((rawBytes[originbyteIdx + 4] & 0x40) >> 3) |
                                    ((rawBytes[originbyteIdx + 5] & 0x40) >> 4) |
                                    ((rawBytes[originbyteIdx + 6] & 0x40) >> 5) |
                                    ((rawBytes[originbyteIdx + 7] & 0x40) >> 6));
                        tempData_16[10] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x20) << 2) |
                                 ((rawBytes[originbyteIdx + 9] & 0x20) << 1) |
                                 ((rawBytes[originbyteIdx + 10] & 0x20)) |
                                 ((rawBytes[originbyteIdx + 11] & 0x20) >> 1) |
                                 ((rawBytes[originbyteIdx + 12] & 0x20) >> 2) |
                                 ((rawBytes[originbyteIdx + 13] & 0x20) >> 3) |
                                 ((rawBytes[originbyteIdx + 14] & 0x20) >> 4) |
                                 ((rawBytes[originbyteIdx + 15] & 0x20) >> 5));
                        tempData_16[11] =
                            (byte) (((rawBytes[originbyteIdx] & 0x20) << 2) |
                                    ((rawBytes[originbyteIdx + 1] & 0x20) << 1) |
                                    ((rawBytes[originbyteIdx + 2] & 0x20)) |
                                    ((rawBytes[originbyteIdx + 3] & 0x20) >> 1) |
                                    ((rawBytes[originbyteIdx + 4] & 0x20) >> 2) |
                                    ((rawBytes[originbyteIdx + 5] & 0x20) >> 3) |
                                    ((rawBytes[originbyteIdx + 6] & 0x20) >> 4) |
                                    ((rawBytes[originbyteIdx + 7] & 0x20) >> 5));
                        tempData_16[8] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x10) << 3) |
                                 ((rawBytes[originbyteIdx + 9] & 0x10) << 2) |
                                 ((rawBytes[originbyteIdx + 10] & 0x10) << 1) |
                                 ((rawBytes[originbyteIdx + 11] & 0x10)) |
                                 ((rawBytes[originbyteIdx + 12] & 0x10) >> 1) |
                                 ((rawBytes[originbyteIdx + 13] & 0x10) >> 2) |
                                 ((rawBytes[originbyteIdx + 14] & 0x10) >> 3) |
                                 ((rawBytes[originbyteIdx + 15] & 0x10) >> 4));
                        tempData_16[9] =
                            (byte) (((rawBytes[originbyteIdx] & 0x10) << 3) |
                                    ((rawBytes[originbyteIdx + 1] & 0x10) << 2) |
                                    ((rawBytes[originbyteIdx + 2] & 0x10) << 1) |
                                    ((rawBytes[originbyteIdx + 3] & 0x10)) |
                                    ((rawBytes[originbyteIdx + 4] & 0x10) >> 1) |
                                    ((rawBytes[originbyteIdx + 5] & 0x10) >> 2) |
                                    ((rawBytes[originbyteIdx + 6] & 0x10) >> 3) |
                                    ((rawBytes[originbyteIdx + 7] & 0x10) >> 4));
                        tempData_16[6] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x08) << 4) |
                                 ((rawBytes[originbyteIdx + 9] & 0x08) << 3) |
                                 ((rawBytes[originbyteIdx + 10] & 0x08) << 2) |
                                 ((rawBytes[originbyteIdx + 11] & 0x08) << 1) |
                                 ((rawBytes[originbyteIdx + 12] & 0x08)) |
                                 ((rawBytes[originbyteIdx + 13] & 0x08) >> 1) |
                                 ((rawBytes[originbyteIdx + 14] & 0x08) >> 2) |
                                 ((rawBytes[originbyteIdx + 15] & 0x08) >> 3));
                        tempData_16[7] =
                            (byte) (((rawBytes[originbyteIdx] & 0x08) << 4) |
                                    ((rawBytes[originbyteIdx + 1] & 0x08) << 3) |
                                    ((rawBytes[originbyteIdx + 2] & 0x08) << 2) |
                                    ((rawBytes[originbyteIdx + 3] & 0x08) << 1) |
                                    ((rawBytes[originbyteIdx + 4] & 0x08)) |
                                    ((rawBytes[originbyteIdx + 5] & 0x08) >> 1) |
                                    ((rawBytes[originbyteIdx + 6] & 0x08) >> 2) |
                                    ((rawBytes[originbyteIdx + 7] & 0x08) >> 3));
                        tempData_16[4] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x04) << 5) |
                                 ((rawBytes[originbyteIdx + 9] & 0x04) << 4) |
                                 ((rawBytes[originbyteIdx + 10] & 0x04) << 3) |
                                 ((rawBytes[originbyteIdx + 11] & 0x04) << 2) |
                                 ((rawBytes[originbyteIdx + 12] & 0x04) << 1) |
                                 ((rawBytes[originbyteIdx + 13] & 0x04)) |
                                 ((rawBytes[originbyteIdx + 14] & 0x04) >> 1) |
                                 ((rawBytes[originbyteIdx + 15] & 0x04) >> 2));
                        tempData_16[5] =
                            (byte) (((rawBytes[originbyteIdx] & 0x04) << 5) |
                                    ((rawBytes[originbyteIdx + 1] & 0x04) << 4) |
                                    ((rawBytes[originbyteIdx + 2] & 0x04) << 3) |
                                    ((rawBytes[originbyteIdx + 3] & 0x04) << 2) |
                                    ((rawBytes[originbyteIdx + 4] & 0x04) << 1) |
                                    ((rawBytes[originbyteIdx + 5] & 0x04)) |
                                    ((rawBytes[originbyteIdx + 6] & 0x04) >> 1) |
                                    ((rawBytes[originbyteIdx + 7] & 0x04) >> 2));
                        tempData_16[2] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x02) << 6) |
                                 ((rawBytes[originbyteIdx + 9] & 0x02) << 5) |
                                 ((rawBytes[originbyteIdx + 10] & 0x02) << 4) |
                                 ((rawBytes[originbyteIdx + 11] & 0x02) << 3) |
                                 ((rawBytes[originbyteIdx + 12] & 0x02) << 2) |
                                 ((rawBytes[originbyteIdx + 13] & 0x02) << 1) |
                                 ((rawBytes[originbyteIdx + 14] & 0x02)) |
                                 ((rawBytes[originbyteIdx + 15] & 0x02) >> 1));
                        tempData_16[3] =
                            (byte) (((rawBytes[originbyteIdx] & 0x02) << 6) |
                                    ((rawBytes[originbyteIdx + 1] & 0x02) << 5) |
                                    ((rawBytes[originbyteIdx + 2] & 0x02) << 4) |
                                    ((rawBytes[originbyteIdx + 3] & 0x02) << 3) |
                                    ((rawBytes[originbyteIdx + 4] & 0x02) << 2) |
                                    ((rawBytes[originbyteIdx + 5] & 0x02) << 1) |
                                    ((rawBytes[originbyteIdx + 6] & 0x02)) |
                                    ((rawBytes[originbyteIdx + 7] & 0x02) >> 1));
                        tempData_16[0] =
                            (byte)
                                (((rawBytes[originbyteIdx + 8] & 0x01) << 7) |
                                 ((rawBytes[originbyteIdx + 9] & 0x01) << 6) |
                                 ((rawBytes[originbyteIdx + 10] & 0x01) << 5) |
                                 ((rawBytes[originbyteIdx + 11] & 0x01) << 4) |
                                 ((rawBytes[originbyteIdx + 12] & 0x01) << 3) |
                                 ((rawBytes[originbyteIdx + 13] & 0x01) << 2) |
                                 ((rawBytes[originbyteIdx + 14] & 0x01) << 1) |
                                 ((rawBytes[originbyteIdx + 15] & 0x01)));
                        tempData_16[1] =
                            (byte) (((rawBytes[originbyteIdx] & 0x01) << 7) |
                                    ((rawBytes[originbyteIdx + 1] & 0x01) << 6) |
                                    ((rawBytes[originbyteIdx + 2] & 0x01) << 5) |
                                    ((rawBytes[originbyteIdx + 3] & 0x01) << 4) |
                                    ((rawBytes[originbyteIdx + 4] & 0x01) << 3) |
                                    ((rawBytes[originbyteIdx + 5] & 0x01) << 2) |
                                    ((rawBytes[originbyteIdx + 6] & 0x01) << 1) |
                                    ((rawBytes[originbyteIdx + 7] & 0x01)));

                        choppedData.AddRange(tempData_64);
                    }

#endregion

                }

                if (originbyteIdx != rawBytes.Length)
                {
                    rawBytes.CopyTo(_chopLeftOver, originbyteIdx);
                }
                else
                    _chopLeftOver = null;

                return choppedData.ToArray();
                //}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = Environment.ProcessorCount});
            });

#endregion

#region Broadcast to display and File save blocks
            var rawDataBroadCast = new BroadcastBlock<byte[]>(null);
#endregion

#region Neural Signal data sink to file (def. Skip)

            _writeRawData = new ActionBlock<byte[]>(async data =>
            {
                //if (data.Length != 0)
                if (!IsDemo && IsSaveResults && (_fileSaveMode == FileSaveModeList.AllRawData))
                {
                    //await Outputfile.WriteAsync(data, 0, data.Length);
                    //Outputfile.Write(bytes, 0, bytes.Length);

                    //Parallel.ForEach(data, value =>
                    //foreach(var value in data)
                    //    {
                    // Note: Synchronous, single thread seems to exit much faster after pressing STOP.
                    //OutputWriter.Write(value);
                    //Outputfile.Write(BitConverter.GetBytes(value), 0, 2);
                    //syncOutputfile.WriteAsync(BitConverter.GetBytes(value), 0, 2);
                    //OutputWriter.Write(value);
                    //_rawDataStream.Write(data, 0, data.Length);
                    if (data != null)
                    {
                        await _rawDataStream.WriteAsync(data, 0, data.Length);
                        //Outputfile.Write(data.ToArray(), 0, data.Count);
                        _writeDataLength += (ulong) data.Length;
                    }
                }
                //);
                //}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = Environment.ProcessorCount});
            });


#endregion

#region Extract info based on mode

            var infoExtract = new TransformBlock<byte[], NeuralChannel.RawData[]>(data =>
            {
                uint addr;
                uint channel;
                ushort[] rawData = new ushort[data.Length / 2];
                //ushort rawData;
                
                List<NeuralChannel.RawData> rawDataList = new List<NeuralChannel.RawData>();

                Buffer.BlockCopy(data, 0, rawData, 0, data.Length);

                //Debug.WriteLine("MainWindow has data of size " + (uint)data.Length);

                //rawData = Enumerable.Range(0, data.Length / 2)
                //    .Select(i => BitConverter.ToUInt16(data, i * 2))
                //    .ToArray();

                //if (!IsDemo)
                //{
                //for (var i = 0; i < data.Length; i += 2)
                for (var i = 0; i < data.Length / 2; i++)
                    {
                        addr = (uint)(i % 32);
                        //if (addr >= 31)
                        //    addr = 0;
                        //else
                        //    addr++;
                        //rawData = BitConverter.ToUInt16(data, i);
                        //switch ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus)

                        if (rawData[i] != 0xFFFF && rawData[i] != 0x53CA)
                        {
                            switch (rawData[i] & 0xC000)
                            {
                                case 0x0000: // a raw sample – 5 bits channel then 9 bits from the intan
                                    channel =
                                        (uint) ((rawData[i] & (ushort) FX3_IGLOO_Nano.RawSignalMask.Channel_Mask) >> 9);
                                    if (NeuralChannel[addr, channel] != null)
                                    {
                                        _runningTime++;
                                        rawDataList.Add(new NeuralChannel.RawData(false, addr, channel, 0, rawData[i]));
                                    }
                                    break;
                                case 0x4000:
                                    // a spike was detected and then 2 bits set high (but change easily), lowest 12 bits give the spike details (2 template id, 5 channel, 5 timestamp)
                                    channel = (uint)((rawData[i] & (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Channel_Mask) >> 5);
                                    var time = TimeBase + (ulong)(rawData[i] & (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Spike_Time_Mask);

                                    if (NeuralChannel[addr, channel] != null)
                                    {
                                        rawDataList.Add(new NeuralChannel.RawData(true, addr, channel, time, rawData[i]));
                                        //Debug.WriteLine("MainWindow has spike at addr " + addr + " channel " + channel); 
                                    }
                                    break;
                                case 0x8000:
                                    // means there is no data to send so it sends a timestamp, lowest 5 bits are timestamp  (these are the 8000’s)
                                    break;
                                case 0xc000: // means an overflow of the timestamp  (these are the C000’s)
                                    // 2ms time tick increment (5-bit increment)
                                    TimeBase += 32;
                                    // Use channel 33 for time tick
                                    try
                                    {
                                        rawDataList.Add(new NeuralChannel.RawData(false, addr, 33, TimeBase, rawData[i]));
                                    }
                                    catch (Exception exception)
                                    {
                                        MessageBox.Show(exception.ToString());
                                    }
                                    break;
                                default:
                                    MessageBox.Show("unrecognised data received on USB");
                                break;
                            }
                        }
                    }

                return rawDataList.ToArray();
            });
#endregion

#region Broadcast to display and File save blocks
            var infoBroadCast = new BroadcastBlock<NeuralChannel.RawData[]>(null);
#endregion

#region Data cast into NeuralChannel Class for display (spike or raw signal)

            _dataCasting = new ActionBlock<NeuralChannel.RawData[]>(data =>
            {
                //if (!IsDemo && (IsLiveViewRunning || (_cfgWindow!=null && _cfgWindow.IsSpikePlotting)))
                //if (!IsDemo)
                //{
                foreach (NeuralChannel.RawData rawData in data)
                {
                    uint addr = rawData.Addr;
                    uint channel = rawData.Channel;

                    //if ((IsLiveViewRunning && _resultsWindow != null && addr == _resultsWindow.GetDisplayAddr())
                    //    ||
                    //    (_cfgWindow != null && _cfgWindow.IsSpikePlotting &&
                    //     addr == CurrentDeviceAddress))

                    //if (channel == 33)
                    //{
                    //    // add a dummy spike event (template 4) for the C0000 in all channels
                    //    for (int i = 0; i < 32; i++)
                    //    {
                    //        NeuralChannel[addr, i].SpikeEvents.Enqueue(
                    //            new NeuralChannel.SpikeData(rawData.Time, 4));
                    //        NeuralChannel[addr, i].LastEventTime = rawData.Time;
                    //    }
                    //}
                    //else 
                    if ((channel != 33) && ((NeuralChannel[addr, channel].IsDisplaying || NeuralChannel[addr, channel].IsSpikeDetecting)))
                    {
                        // Channel 33 reserved for time tick signal

                        ushort inputData = rawData.OriginalData;
                        int sample;
                        ulong spikeTime;
                        int templateId;

                        switch ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus)
                        {
                            case FX3_IGLOO_Nano.Status.ReadOut:
                                if (rawData.IsSpikeEvent)
                                {

                                    Debug.WriteLine("Mainwindow is passing spike from channel " + channel + " addrs " + addr);
                                    // Spike event
                                    spikeTime = rawData.Time;
                                    templateId = (inputData &
                                                  (ushort)FX3_IGLOO_Nano.SpikeSignalMask.Template_Mask) >>
                                                 10;

                                    NeuralChannel[addr, channel].SpikeEvents.Enqueue(
                                        new NeuralChannel.SpikeData(spikeTime, (ushort)(templateId)));

                                    NeuralChannel[addr, channel].LastEventTime = spikeTime;
                                }
                                else
                                {
                                    // Neural signal
                                    sample = inputData & (ushort)FX3_IGLOO_Nano.RawSignalMask.Value_Mask;

                                    if (NeuralChannel[addr, channel].IsDisplaying)
                                        NeuralChannel[addr, channel].NeuralSignals.Enqueue(
                                            new NeuralChannel.NeuralSignal(_runningTime / 32,
                                                (ushort)(sample)));
                                    if (NeuralChannel[addr, channel].IsSpikeDetecting)
                                        NeuralChannel[addr, channel].SpikeScopeFIFO.Enqueue(
                                            new NeuralChannel.NeuralSignal(_runningTime / 32,
                                                (ushort)(sample)));
                                }

                                break;
                            case FX3_IGLOO_Nano.Status.Disconnected:
                                // Demo mode:
                                if (!IsSpikeSorting)
                                {
                                    sample = inputData & (ushort) FX3_IGLOO_Nano.RawSignalMask.Value_Mask;

                                    if (NeuralChannel[addr, channel].IsDisplaying)
                                        NeuralChannel[addr, channel].NeuralSignals.Enqueue(
                                            new NeuralChannel.NeuralSignal(_runningTime / 32,
                                                (ushort) (sample)));
                                    if (NeuralChannel[addr, channel].IsSpikeDetecting)
                                        NeuralChannel[addr, channel].SpikeScopeFIFO.Enqueue(
                                            new NeuralChannel.NeuralSignal(_runningTime / 32,
                                                (ushort) (sample)));
                                }
                                else
                                {
                                    spikeTime = rawData.Time;
                                    templateId = (inputData &
                                                  (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Template_Mask) >>
                                                 10;

                                    NeuralChannel[addr, channel].SpikeEvents.Enqueue(
                                        new NeuralChannel.SpikeData(spikeTime, (ushort) (templateId)));

                                    NeuralChannel[addr, channel].LastEventTime = spikeTime;
                                }
                                break;
                        }
                    }
                    //if (channel != 33) _runningTime++;  //Keep track of number of samples
                }
                //}
                ////else if (IsDemo && ((IsLiveViewRunning && _resultsWindow != null) || (_cfgWindow != null && _cfgWindow.IsSpikePlotting)))
                //else
                //{
                //    if (IsSpikeSorting)
                //    {
                //        foreach (NeuralChannel.RawData rawData in data)
                //        {
                //            uint addr = rawData.Addr;
                //            uint channel = rawData.Channel;

                //            if (NeuralChannel[addr, channel].IsDisplaying ||
                //                NeuralChannel[addr, channel].IsSpikeDetecting)
                //            {
                //                var inputData = rawData.OriginalData;
                //                var sample = (inputData & (0x1FF << 5)) >> 5;

                //                if (NeuralChannel[addr, channel].IsDisplaying)
                //                    NeuralChannel[addr, channel].NeuralSignals.Enqueue(
                //                        new NeuralChannel.NeuralSignal(_runningTime / 32,
                //                            (ushort)(sample)));
                //                if (NeuralChannel[addr, channel].IsSpikeDetecting)
                //                    NeuralChannel[addr, channel].SpikeScopeFIFO.Enqueue(
                //                        new NeuralChannel.NeuralSignal(_runningTime / 32,
                //                            (ushort)(sample)));
                //            }
                //            _runningTime++;
                //        }
                //    }
                //    else
                //    {
                //        foreach (NeuralChannel.RawData rawData in data)
                //        {
                //            uint addr = rawData.Addr;
                //            uint channel = rawData.Channel;

                //            if (NeuralChannel[addr, channel].IsDisplaying ||
                //                NeuralChannel[addr, channel].IsSpikeDetecting)
                //            {
                //                var inputData = rawData.OriginalData;
                //                var sample = (inputData & (0x1FF << 5)) >> 5;

                //                if (NeuralChannel[addr, channel].IsDisplaying)
                //                    NeuralChannel[addr, channel].NeuralSignals.Enqueue(
                //                        new NeuralChannel.NeuralSignal(_runningTime / 32,
                //                            (ushort)(sample)));
                //                if (NeuralChannel[addr, channel].IsSpikeDetecting)
                //                    NeuralChannel[addr, channel].SpikeScopeFIFO.Enqueue(
                //                        new NeuralChannel.NeuralSignal(_runningTime / 32,
                //                            (ushort)(sample)));
                //            }
                //            _runningTime++;
                //        }
                //    }
                //}
                //
            }, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = Environment.ProcessorCount});

#endregion

#region Write results to files (per Channel or per Address)

            _writeRestuls = new ActionBlock<NeuralChannel.RawData[]>(async data =>
            {
                if (!IsDemo && IsSaveResults)
                {
                    DataSavingLock.WaitOne();
                    foreach (var rawData in data)
                        //Parallel.ForEach(data, async rawData =>
                    {
                        try
                        {
                            byte[] output = null;
                            byte[] output_spike = null;

                            switch (_fileSaveMode)
                            {
                                case FileSaveModeList.PerChannel:
                                    // AB - removing this feature. Output will include timer overflow
                                    //if (rawData.Channel != 33) // Don't save time tick signal
                                    if(true)    // AB - this is becuase I can't bring myself to remove these brakets. What did they ever do to you? Leave them alone! 
                                    {
                                        lock (FileLock[rawData.Addr, rawData.Channel])
                                        {
                                            if (rawData.IsSpikeEvent)
                                            {
                                                // Add calculated time at the beginning for template matching output.
                                                NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Add(
                                                    (ushort) (rawData.Time & 0xFFFF));
                                                NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 16) & 0xFFFF));
                                                NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 32) & 0xFFFF));
                                                NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 48) & 0xFFFF));

                                                NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Add(
                                                    rawData.OriginalData);

                                                if (
                                                    NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Count >=
                                                    65536)
                                                {
                                                    output_spike =
                                                        NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream
                                                            .SelectMany(
                                                                BitConverter.GetBytes).ToArray();
                                                    NeuralChannel[rawData.Addr, rawData.Channel].SpikeCacheStream.Clear();
                                                }
                                            }
                                            else
                                            {
                                                NeuralChannel[rawData.Addr, rawData.Channel].CacheStream.Add(
                                                    rawData.OriginalData);

                                                if (NeuralChannel[rawData.Addr, rawData.Channel].CacheStream.Count >=
                                                    65536)
                                                {
                                                    output =
                                                        NeuralChannel[rawData.Addr, rawData.Channel].CacheStream
                                                            .SelectMany(
                                                                BitConverter.GetBytes).ToArray();
                                                    NeuralChannel[rawData.Addr, rawData.Channel].CacheStream.Clear();
                                                }
                                            }
                                        }

                                        if (output_spike != null)
                                        {
                                            await
                                                OutputSpikeWriter[rawData.Addr, rawData.Channel].WriteAsync(output_spike, 0,
                                                    output_spike.Length);
                                        }

                                        if (output != null)
                                        {
                                            await
                                                OutputWriter[rawData.Addr, rawData.Channel].WriteAsync(output, 0,
                                                    output.Length);
                                        }
                                    }//You monster....
                                    break;
                                case FileSaveModeList.PerAddress:
                                    if (rawData.Channel != 33) // Don't save time tick signal
                                    {
                                        lock (FileLock[rawData.Addr, 0])
                                        {
                                            if (rawData.IsSpikeEvent)
                                            {
                                                // Add calculated time at the beginning for template matching output.
                                                NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Add(
                                                    (ushort) (rawData.Time & 0xFFFF));
                                                NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 16) & 0xFFFF));
                                                NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 32) & 0xFFFF));
                                                NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Add(
                                                    (ushort) ((rawData.Time >> 48) & 0xFFFF));

                                                NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Add(rawData.OriginalData);

                                                if (NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Count >= 4096)
                                                {
                                                    output_spike =
                                                        NeuralChannel[rawData.Addr, 0].SpikeCacheStream.SelectMany(
                                                            BitConverter.GetBytes).ToArray();
                                                    NeuralChannel[rawData.Addr, 0].SpikeCacheStream.Clear();
                                                }
                                            }
                                            else
                                            {
                                                NeuralChannel[rawData.Addr, 0].CacheStream.Add(rawData.OriginalData);

                                                if (NeuralChannel[rawData.Addr, 0].CacheStream.Count >= 65536)
                                                {
                                                    output =
                                                        NeuralChannel[rawData.Addr, 0].CacheStream.SelectMany(
                                                            BitConverter.GetBytes).ToArray();
                                                    NeuralChannel[rawData.Addr, 0].CacheStream.Clear();
                                                }
                                            }
                                        }

                                        if (output_spike != null)
                                        {
                                            await OutputSpikeWriter[rawData.Addr, 0].WriteAsync(output_spike, 0, output_spike.Length);
                                        }

                                        if (output != null)
                                        {
                                            await OutputWriter[rawData.Addr, 0].WriteAsync(output, 0, output.Length);
                                        }

                                    }
                                    break;
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show("Error while saving file. Info:" + exception.Message.ToString());
                        }
                    }
                    DataSavingLock.Release(1);
                }
                //}, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount});
            });
#endregion

#region Forming the pipeline

            if (IsDemo)
            {
                buffer.LinkTo(infoExtract);
                infoExtract.LinkTo(_dataCasting);
            }
            else
            {
                buffer.LinkTo(transpose);
                transpose.LinkTo(rawDataBroadCast);
                rawDataBroadCast.LinkTo(_writeRawData);
                rawDataBroadCast.LinkTo(infoExtract);
                infoExtract.LinkTo(infoBroadCast);
                infoBroadCast.LinkTo(_dataCasting);
                infoBroadCast.LinkTo(_writeRestuls);
            }

#endregion Forming the pipeline

#region Fault & Completion Procedure

            if (!IsDemo)
            {
                buffer.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock) transpose).Fault(t.Exception);
                    else transpose.Complete();
                    //else broadCast.Complete();
                });

                transpose.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)rawDataBroadCast).Fault(t.Exception);
                    else rawDataBroadCast.Complete();
                });

                rawDataBroadCast.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ((IDataflowBlock)_writeRawData).Fault(t.Exception);
                        ((IDataflowBlock)infoExtract).Fault(t.Exception);
                    }
                    else
                    {
                        _writeRawData.Complete();
                        infoExtract.Complete();
                    }
                });


                infoExtract.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)infoBroadCast).Fault(t.Exception);
                    else infoBroadCast.Complete();

                });


                infoBroadCast.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ((IDataflowBlock)_dataCasting).Fault(t.Exception);
                        ((IDataflowBlock)_writeRestuls).Fault(t.Exception);
                    }
                    else
                    {
                        _dataCasting.Complete();
                        _writeRestuls.Complete();
                    }
                });
            }
            else
            {
                buffer.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)infoExtract).Fault(t.Exception);
                    else infoExtract.Complete();
                    //else broadCast.Complete();
                });

                infoExtract.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)_dataCasting).Fault(t.Exception);
                    else _dataCasting.Complete();

                });
            }

            /* Debug */

            //buffer.Completion.ContinueWith(t =>
            //{
            //    if (t.IsFaulted) ((IDataflowBlock)writeToFile).Fault(t.Exception);
            //    else chopAndTranspose.Complete();
            //});

            //buffer.Completion.ContinueWith(t =>
            //{
            //    if (t.IsFaulted) ((IDataflowBlock)dataCasting).Fault(t.Exception);
            //    else dataCasting.Complete();
            //});

#endregion Fault & Completion Procedure
        }

        private void ToggleSaveResults(object sender, RoutedEventArgs e)
        {
            //UpdateSaveFilePath(ProjectFolder, GetFileNameWithExtension());
            if (IsSaveResults)
            {
                IsSaveResults = !IsSaveResults; // Make sure stopping saving before closing files
                DataSavingLock.WaitOne();
                try
                {
                    if (_fileSaveMode == FileSaveModeList.AllRawData)
                    {
                        //_writeRawData.Completion.Wait();
                        // Close raw data stream
                        //_rawDataStream.Flush();
                        {
                            //_rawDataStream.Flush();
                            _rawDataStream.Close();
                            _rawDataStream = null;
                        }
                    }
                    else if (_fileSaveMode == FileSaveModeList.PerChannel)
                    {
                        //_writeRestuls.Completion.Wait();

                        // Close all data stream
                        for (int i = 0; i < 32; i++)
                        {
                            for (int j = 0; j < 32; j++)
                            {
                                if (OutputWriter[i, j] != null)
                                {
                                    if (NeuralChannel[i, j].CacheStream != null &&
                                        NeuralChannel[i, j].CacheStream.Count != 0)
                                    {
                                        // Make sure all the data are written out
                                        var output =
                                            NeuralChannel[i, j].CacheStream.SelectMany(
                                                BitConverter.GetBytes).ToArray();
                                        OutputWriter[i, j].Write(output, 0,
                                            output.Length);
                                    }
                                    //OutputWriter[i,j].Flush();
                                    OutputWriter[i, j].Close();
                                    OutputWriter[i, j] = null;
                                }

                                if (OutputSpikeWriter[i, j] != null)
                                {
                                    if (NeuralChannel[i, j].SpikeCacheStream != null &&
                                        NeuralChannel[i, j].SpikeCacheStream.Count != 0)
                                    {
                                        // Make sure all the data are written out
                                        var output =
                                            NeuralChannel[i, j].SpikeCacheStream.SelectMany(
                                                BitConverter.GetBytes).ToArray();
                                        OutputSpikeWriter[i, j].Write(output, 0,
                                            output.Length);
                                    }
                                    //OutputWriter[i,j].Flush();
                                    OutputSpikeWriter[i, j].Close();
                                    OutputSpikeWriter[i, j] = null;
                                }
                            }
                        }
                    }
                    else if (_fileSaveMode == FileSaveModeList.PerAddress)
                    {
                        //_writeRestuls.Completion.Wait();

                        // Close all data stream
                        for (int i = 0; i < 32; i++)
                        {
                            if (OutputWriter[i, 0] != null)
                            {
                                for (int j = 0; j < 32; j++)
                                {
                                    // Dump all the channels on this address
                                    if (NeuralChannel[i, j].CacheStream != null &&
                                        NeuralChannel[i, j].CacheStream.Count != 0)
                                    {
                                        // Make sure all the data are written out
                                        var output =
                                            NeuralChannel[i, j].CacheStream.SelectMany(
                                                BitConverter.GetBytes).ToArray();
                                        NeuralChannel[i, j].CacheStream.Clear();
                                        OutputWriter[i, 0].Write(output, 0,
                                            output.Length);
                                    }
                                }
                                //OutputWriter[i, 0].Flush();
                                OutputWriter[i, 0].Close();
                                OutputWriter[i, 0] = null;
                            }
                            if (OutputSpikeWriter[i, 0] != null)
                            {
                                for (int j = 0; j < 32; j++)
                                {
                                    // Dump all the channels on this address
                                    if (NeuralChannel[i, j].SpikeCacheStream != null &&
                                        NeuralChannel[i, j].SpikeCacheStream.Count != 0)
                                    {
                                        // Make sure all the data are written out
                                        var output =
                                            NeuralChannel[i, j].SpikeCacheStream.SelectMany(
                                                BitConverter.GetBytes).ToArray();
                                        NeuralChannel[i, j].SpikeCacheStream.Clear();
                                        OutputSpikeWriter[i, 0].Write(output, 0,
                                            output.Length);
                                    }
                                }
                                //OutputWriter[i, 0].Flush();
                                OutputSpikeWriter[i, 0].Close();
                                OutputSpikeWriter[i, 0] = null;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(@"Error while closing file handels.Info:" + exception.Message.ToString());
                }

#if DEBUG
                Console.WriteLine(@"Source Length {0:d}, Desination Length {1:d} (0x8000 data {2:d} bytes)",
                    USBridge.InputDataLength, _writeDataLength + _invalidDataLength, _invalidDataLength);
#endif
                DataSavingLock.Release();
            }
            else
            {
                CreateFiles();
                IsSaveResults = !IsSaveResults; // Make sure file locks are ready before start saving.
            }
        }

        public void Start_Streaming()
        {
            //var fileName = Path.GetFileName(FilePath.Text);

            //if (directoryName != null &&
            //    (directoryName.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1) &&
            //    (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1))

            if (ProjectFolder != null && (ProjectFolder.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1))
            {
                MessageBox.Show(@"Invalid character detected in file path!");
                return;
            }

            //Btn_Stop.IsEnabled = true;
            //Btn_Start.IsEnabled = false;
            //Btn_Cfg.IsEnabled = false;
            //Btn_SaveFile.IsEnabled = false;
            //Btn_ModeToggle.IsEnabled = false;
            //Btn_Rst.IsEnabled = false;
#if MultiAddr
            for (var addr = 0; addr < 32; addr++)
                for (var channelId = 0; channelId < 32; channelId++)
                {
                    if (NeuralChannel[addr, channelId] == null) continue;

                    // Reset timestamp
                    NeuralChannel[addr, channelId].LastEventTime = 0;
                }
#else
            for (var channelId = 0; channelId < 32; channelId++)
            {
                if (NeuralChannel[CurrentDeviceAddress, channelId] == null) continue;

                // Reset timestamp
                NeuralChannel[CurrentDeviceAddress, channelId].LastEventTime = 0;
            }

            // Catchup spike event timebase
            TimeBase = _runningTime / 32;

            IsStreaming = true;
#endif

            // When load the template on the run, these should not be reset.
            //_runningTime = 0;
            //TimeBase = 0;

            //CreateFiles();  

            if (!IsDemo)
            {
#if MultiAddr
                for (int addr = 0; addr < 32; addr++)
                {
                    if (INTAN[addr].Connected)
                    {
                        switch ((FX3_IGLOO_Nano.Status)USBridge.DeviceStatus)
                        {
                            case FX3_IGLOO_Nano.Status.PC_sig:
                                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO,
                                    FX3_IGLOO_Nano.Status.ReadOut, addr);
                                break;
                            case FX3_IGLOO_Nano.Status.Idle_PC_spike:
                                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO,
                                    FX3_IGLOO_Nano.Status.ReadOut_PC_spike, addr);
                                break;
                        }
                    }
                }
#else
                if (INTAN[CurrentDeviceAddress].Connected)
                {
                    USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.ReadOut, CurrentDeviceAddress);
                }
#endif

                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.FX3, FX3.Status.StreamIn, CurrentDeviceAddress);
                //Address is ignored by FX3 firmware
            }

            //syncOutputfile = Stream.Synchronized(Outputfile);
            //OutputWriter = new BinaryWriter(Stream.Synchronized(Outputfile));
            //OutputWriter = new BinaryWriter(Outputfile);


            BuildPipeline();

            // Start feeding the data

            _tListen = IsDemo
                ? Task.Factory.StartNew(() => { SyntheticSignal.StartInputDataFlow(); })
                : Task.Factory.StartNew(() => { USBridge.StartInputDataFlow(); });

        }

        private void CreateFiles()
        {
            // Update file path
            UpdateSaveFilePath(ProjectFolder, $"{DateTime.Now:yyyy-MM-dd_hh-mm-ss-tt}");
            // Create file handles

            if (ProjectFolder != null && !Directory.Exists(ProjectFolder))
                Directory.CreateDirectory(ProjectFolder);

            switch (_fileSaveMode)
            {
                case FileSaveModeList.AllRawData:
                    //if (!Directory.Exists(Path.GetDirectoryName(_rawDataFile)))
                    //    Directory.CreateDirectory(Path.GetDirectoryName(_rawDataFile));
                    if (_rawDataStream == null)
                    {
                        _rawDataStream = new FileStream(_rawDataFile, FileMode.Create, FileAccess.Write, FileShare.Write,
                            4096, true);
                    }
                    break;
                case FileSaveModeList.PerChannel:
                    //if (!Directory.Exists(Path.GetDirectoryName(_perChannelResultFile[0, 0])))
                    //    Directory.CreateDirectory(Path.GetDirectoryName(_perChannelResultFile[0, 0]));
                    foreach (var addr in ActiveDeviceAddressList)
                        for (var channelId = 0; channelId < 32; channelId++)
                        {
                            if (NeuralChannel[addr.Value, channelId] == null) continue;

                            // Open file handels
                            if (OutputWriter[addr.Value, channelId] == null)
                            {
                                //OutputWriter[0, channelId] = 
                                //    File.Create(perChannelResultFile[channelId], 4, FileOptions.Asynchronous);

                                OutputWriter[addr.Value, channelId] =
                                    new FileStream(_perChannelResultFile[addr.Value, channelId],
                                        FileMode.Create,
                                        FileAccess.Write, FileShare.None, 4096, true);
                            }

                            if (OutputSpikeWriter[addr.Value, channelId] == null)
                            {
                                //OutputWriter[0, channelId] = 
                                //    File.Create(perChannelResultFile[channelId], 4, FileOptions.Asynchronous);

                                OutputSpikeWriter[addr.Value, channelId] =
                                    new FileStream(_perChannelSpikeResultFile[addr.Value, channelId],
                                        FileMode.Create,
                                        FileAccess.Write, FileShare.None, 4096, true);
                            }

                            if (FileLock[addr.Value, channelId] == null)
                            { 
                                FileLock[addr.Value, channelId] = new object();
                            }
                        }
                    break;
                case FileSaveModeList.PerAddress:
                    //if (!Directory.Exists(Path.GetDirectoryName(_perAddrResultFile[0])))
                    //    Directory.CreateDirectory(Path.GetDirectoryName(_perAddrResultFile[0]));
                    //for (var addr = 0; addr < 32; addr++)
                    foreach (var addr in ActiveDeviceAddressList)
                    {
                        if (!INTAN[addr.Value].Connected) continue;
                        // Open file handels
                        if (((int) PC_RecMode.SelectedValue == (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Sig) && OutputWriter[addr.Value, 0] == null)
                        {
                            OutputWriter[addr.Value, 0] = new FileStream(_perAddrResultFile[addr.Value], FileMode.Create,
                                FileAccess.Write, FileShare.None, 4096, true);
                        }

                        if (((int)PC_RecMode.SelectedValue == (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Spike) && OutputSpikeWriter[addr.Value, 0] == null)
                        {
                            OutputSpikeWriter[addr.Value, 0] = new FileStream(_perAddrSpikeResultFile[addr.Value], FileMode.Create,
                                FileAccess.Write, FileShare.None, 4096, true);
                        }

                        if (((int)PC_RecMode.SelectedValue == ((int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Sig | (int)FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Spike)) && OutputSpikeWriter[addr.Value, 0] == null && OutputWriter[addr.Value, 0] == null)
                        {
                            OutputWriter[addr.Value, 0] = new FileStream(_perAddrResultFile[addr.Value], FileMode.Create,
                                FileAccess.Write, FileShare.None, 4096, true);
                            OutputSpikeWriter[addr.Value, 0] = new FileStream(_perAddrSpikeResultFile[addr.Value], FileMode.Create,
                                FileAccess.Write, FileShare.None, 4096, true);

                        }

                        if (FileLock[addr.Value, 0] == null)
                        {
                            FileLock[addr.Value, 0] = new object();
                        }

                    }
                    break;
            }

            _writeDataLength = 0;
        }

        public void Stop_Streaming(bool isShutdown=false)
        {
            //Btn_Start.IsEnabled = true;
            //Btn_Stop.IsEnabled = false;
            //Btn_Cfg.IsEnabled = true;
            //Btn_SaveFile.IsEnabled = true;
            //Btn_ModeToggle.IsEnabled = true;
            //Btn_Rst.IsEnabled = true;
            IsStreaming = false;

            if (IsDemo)
            {
                SyntheticSignal.StopInputDataFlow();
                // wait for task to complete before closing file
                try
                {
                    _tListen.Wait();
                }
                catch (Exception)
                {   
                    throw new Exception();
                }
            }
            else
            {
                USBridge.StopInputDataFlow();
                // wait for task to complete before closing file
                var status = _tListen.Wait(10000);
                if (status == false)
                    MessageBox.Show(@"Streaming stop error!");
//                if (IsSaveResults)
//                {
//                    if (_fileSaveMode == FileSaveModeList.AllRawData)
//                    {
//                        _writeRawData.Completion.Wait();
//                        // Close raw data stream
//                        //_rawDataStream.Flush();
//                        _rawDataStream.Close();
//                        //_rawDataStream = null;
//                    }

//                    try
//                    {
//                        _dataCasting.Completion.Wait();

//                        if (_fileSaveMode == FileSaveModeList.PerChannel)
//                        {
//                            _writeRestuls.Completion.Wait();

//                            // Close all data stream
//                            for (int i = 0; i < 32; i++)
//                            {
//                                for (int j = 0; j < 32; j++)
//                                {
//                                    if (OutputWriter[i, j] != null)
//                                    {
//                                        if (NeuralChannel[i, j].CacheStream != null &&
//                                            NeuralChannel[i, j].CacheStream.Count != 0)
//                                        {
//                                            // Make sure all the data are written out
//                                            var output =
//                                                NeuralChannel[i, j].CacheStream.SelectMany(
//                                                    BitConverter.GetBytes).ToArray();
//                                            OutputWriter[i, j].Write(output, 0,
//                                                output.Length);
//                                        }
//                                        //OutputWriter[i,j].Flush();
//                                        OutputWriter[i, j].Close();
//                                        OutputWriter[i, j] = null;
//                                    }
//                                }
//                            }
//                        }
//                        else if (_fileSaveMode == FileSaveModeList.PerAddress)
//                        {
//                            _writeRestuls.Completion.Wait();

//                            // Close all data stream
//                            for (int i = 0; i < 32; i++)
//                            {
//                                if (OutputWriter[i, 0] != null)
//                                {
//                                    for (int j = 0; j < 32; j++)
//                                    {
//                                        // Dump all the channels on this address
//                                        if (NeuralChannel[i, j].CacheStream != null &&
//                                            NeuralChannel[i, j].CacheStream.Count != 0)
//                                        {
//                                            // Make sure all the data are written out
//                                            var output =
//                                                NeuralChannel[i, j].CacheStream.SelectMany(
//                                                    BitConverter.GetBytes).ToArray();
//                                            NeuralChannel[i, j].CacheStream.Clear();
//                                            OutputWriter[i, 0].Write(output, 0,
//                                                output.Length);
//                                        }
//                                    }
//                                    //OutputWriter[i, 0].Flush();
//                                    OutputWriter[i, 0].Close();
//                                    OutputWriter[i, 0] = null;
//                                }
//                            }
//                        }
//                    }
//                    catch (Exception exception)
//                    {
//                        MessageBox.Show(@"Error while closing file handels.Info:" + exception.Message.ToString());
//                    }

//#if DEBUG
//                    Console.WriteLine(@"Source Length {0:d}, Desination Length {1:d} (0x8000 data {2:d} bytes)",
//                        USBridge.InputDataLength, _writeDataLength + _invalidDataLength, _invalidDataLength);
//#endif
//                }
                //else
                //{
                    _dataCasting.Completion.Wait();
                //}

                //if (!isShutdown)
                    USBridge.SwitchMode(FX3_IGLOO_Nano.Target.FX3, FX3.Status.Configure, CurrentDeviceAddress);
                Task.Delay(1).Wait();   // FX3 needs time to override GPIO pins controlled by GPIF
                
#if MultiAddr
                for (int addr = 0; addr < 32; addr++)
                {
                    if (INTAN[addr].Connected)
                    {
                        switch ((FX3_IGLOO_Nano.Status) USBridge.DeviceStatus)
                        {
                            case FX3_IGLOO_Nano.Status.ReadOut:
                                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.PC_sig, addr);
                                break;
                            case FX3_IGLOO_Nano.Status.ReadOut_PC_spike:
                                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle_PC_spike, addr);
                                break;
                        }
                    }
                }
#else
                if (INTAN[CurrentDeviceAddress].Connected)
                {
                    USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle, CurrentDeviceAddress);
                    USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, CurrentDeviceAddress); // For FPGA to pass onto the logger. Otherwise, logger would lose track of FPGA's status
                }
#endif
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DialogResult userAnswer = System.Windows.Forms.DialogResult.No;

            if (!IsDemo)
            {
                if (IsStreaming)
                {
                    Stop_Streaming();
                    userAnswer = MessageBox.Show("Close the Logger recording too?", "Exiting USBridge", MessageBoxButtons.YesNo);
                }
                //USBridge.SwitchMode(FX3_IGLOO_Nano.Target.FX3, FX3.Status.Configure, CurrentDeviceAddress);
#if MultiAddr
                for (int addr = 0; addr < 32; addr++)
                {
                    if (INTAN[addr].Connected)
                    {
                        USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                            BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, CurrentDeviceAddress);

                        USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO,
                            FX3_IGLOO_Nano.Status.PC_sig, addr);
                    }
                }
#else
                if (INTAN[CurrentDeviceAddress] != null && INTAN[CurrentDeviceAddress].Connected)
                {
                    if (userAnswer == System.Windows.Forms.DialogResult.Yes)
                    {
                        IsLogging = false;
                        // Closing the file on the logger first
                        USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Stop_Logging), 2, CurrentDeviceAddress);
                        // Disable secondary link on FPGA
                        SetRecMode();
                    }
                }
#endif
            }

            if (USBridge != null) USBridge.Dispose();
            Application.Current.Shutdown();
        }

        public static void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    GetFileNameWithExtension();
        //    //if (FileSaveMode == FileSaveModeList.AllRawData)
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".raw";
        //    //    }
        //    //}
        //    //else if (IsSpikeSorting)
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".spike";
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".sig";
        //    //    }
        //    //}
        //}

        //private void CheckFileInput(object sender, RoutedEventArgs e)
        //{
        //    GetFileNameWithExtension();
        //    //if (FileSaveMode == FileSaveModeList.AllRawData)
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".raw";
        //    //    }
        //    //}
        //    //else if (IsSpikeSorting)
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".spike";
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    if (FilePath != null && FilePath.Text != null)
        //    //    {
        //    //        FilePath.Text = Path.GetDirectoryName(FilePath.Text) + "\\" +
        //    //                        Path.GetFileNameWithoutExtension(FilePath.Text) + ".sig";
        //    //    }
        //    //}
        //}

        //private string GetFileNameWithExtension()
        //{
        //    if (FileSaveMode == FileSaveModeList.AllRawData)
        //    {
        //        if (ProjectFolder != null)
        //        {
        //            return string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".raw";
        //        }
        //    }
        //    else if (IsSpikeSorting)
        //    {
        //        if (ProjectFolder != null)
        //        {
        //            return string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".spike";
        //        }
        //    }
        //    else
        //    {
        //        if (ProjectFolder != null)
        //        {
        //            return string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".sig";
        //        }
        //    }

        //    return null;
        //}

        private void AutoSetup(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
            MessageBox.Show(IsAutoStart.ToString());
        }

        private void ShowAmplifierSetting(object sender, RoutedEventArgs e)
        {
            if (!FlyoutAmpSetting.IsOpen) FlyoutAmpSetting.IsOpen=true;
        }
        
        private void LoadTemplate(object sender, RoutedEventArgs e)
        {
            //Browse
            var openFileDialog1 = new OpenFileDialog
            {
                DefaultExt = ".bin",
                Filter = @"NGNI config (.bin) | *.bin"
            };
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            _cfgFile = openFileDialog1.FileName;
            //Todo embbed device address in template files
            //Load
            load_cfg_file(CurrentDeviceAddress, _cfgFile);

            //Send
            if (IsDemo)
            {
                MessageBox.Show(@"Demo mode: Configuration Send", "Info");
            }
            else
            {
                _cfgStream = generate_cfg_stream(CurrentDeviceAddress);
                //USBridge.SwitchMode(FX3_IGLOO_Nano.Target.FX3, FX3.Status.Configure, 0);
                //if (IsStreaming) USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, CurrentDeviceAddress);
                //USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                //    _cfgStream,
                //    _cfgStream.Length, CurrentDeviceAddress);
                //if (IsStreaming) USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Start_Read_Out), 2, CurrentDeviceAddress);
                //USBridge.SwitchMode(FX3_IGLOO_Nano.Target.FX3, FX3.Status.StreamIn, 0);

                if (IsStreaming)
                {
                    Stop_Streaming();
                    Thread.Sleep(100);
                    USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                        _cfgStream,
                        _cfgStream.Length, CurrentDeviceAddress);
                    MessageBox.Show(@"Templated Uploaded", "Info");
                    Start_Streaming();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            //Refresh UI
            var button = sender as MenuItem;
            if (button != null)
            {
                var command = button.Tag as ICommand;
                if (command != null)
                    command.Execute(null);
            }

        }

        private static bool load_cfg_file(int deviceAddr, string cfgFile = null)
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

        private static byte[] generate_cfg_stream(int deviceAddr)
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

            //// Configure 16 to 9 ADC range mappping
            //cfgStream.AddRange(
            //    BitConverter.GetBytes(
            //        (ushort)
            //            ((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Change_LSB |
            //             (ushort)(INTAN[deviceAddr].LSBMapBit << 12))));
            //cfgStream.Add(0x00);
            //cfgStream.Add(0x00);

            cfgStream.AddRange(BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop));
            cfgStream.Add(0x00);
            cfgStream.Add(0x00);

            return cfgStream.ToArray();
        }


        private void AddWaveformPlot(object sender, RoutedEventArgs e)
        {
            SelectPlotChannelDiag inputDialog = new SelectPlotChannelDiag();
            if (inputDialog.ShowDialog() == true)
            {
                NewAddressId = inputDialog.NewPlotAddressId;
                NewChannelId = inputDialog.NewPlotChannelId;

                if (NewChannelId == 33) // One device
                {
                    var button = sender as MenuItem;
                    if (button != null)
                    {
                        var command = button.Tag as ICommand;
                        if (command != null)
                            command.Execute(new object[2] { NewAddressId, NewChannelId});
                    }

                    RootLayout.Tile();
                }
                else if (!NeuralChannel[NewAddressId, NewChannelId].IsDisplaying)   // One Channel
                {
                    var button = sender as MenuItem;
                    if (button != null)
                    {
                        var command = button.Tag as ICommand;
                        if (command != null)
                            command.Execute(new object[2] {NewAddressId, NewChannelId});
                    }
                }
                else
                {
                    MessageBox.Show("Waveform for Device "+ NewAddressId+",Channel "+NewChannelId+" is being shown.", "Info");
                }
            }
        }

        private void OpenProjectFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(ProjectFolder);
        }

        private void AddSpikePlot(object sender, RoutedEventArgs e)
        {
            SelectPlotChannelDiag inputDialog = new SelectPlotChannelDiag();
            if (inputDialog.ShowDialog() == true)
            {
                NewAddressId = inputDialog.NewPlotAddressId;
                NewChannelId = inputDialog.NewPlotChannelId;

                if (NewChannelId == 33) // One device
                {
                    var button = sender as MenuItem;
                    if (button != null)
                    {
                        var command = button.Tag as ICommand;
                        if (command != null)
                            command.Execute(new object[2] { NewAddressId, NewChannelId});
                    }

                    RootLayout.Tile();
                }
                else if (!NeuralChannel[NewAddressId, NewChannelId].IsSpikeDetecting)   // One Channel
                {
                    var button = sender as MenuItem;
                    if (button != null)
                    {
                        var command = button.Tag as ICommand;
                        if (command != null)
                            command.Execute(new object[2] { NewAddressId, NewChannelId});
                    }
                }
                else
                {
                    MessageBox.Show("Spike plot for Device " + NewAddressId + ",Channel " + NewChannelId + " is being shown.", "Info");
                }
            }
        }

        private void OpenMatlabApp(object sender, RoutedEventArgs e)
        {
            Process app = new Process();

            try
            {
                app.StartInfo.UseShellExecute = false;
                app.StartInfo.FileName = @"C:\Program Files\temp_match_standalone\application\temp_match_standalone.exe";
                //Todo send project folder as parameters
                app.StartInfo.CreateNoWindow = true;
                if (!app.Start())
                {
                    MessageBox.Show("Template genereation app cannot be found!", "Error");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Error starting the app. Info", "Error");
            }
        }

        private void CalibrateADC(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        //private void Enable_AutoStart(object sender, RoutedEventArgs e)
        //{
        //    if (IsStreaming)
        //    {
        //        Stop_Streaming();
        //        Thread.Sleep(10);
        //        USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Enable_AutoStart | Convert.ToUInt16(Logger_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
        //        Thread.Sleep(10);
        //        Start_Streaming();
        //    }
        //    else
        //    {
        //        USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Enable_AutoStart | Convert.ToUInt16(Logger_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
        //    }
        //}

        //private void Disable_AutoStart(object sender, RoutedEventArgs e)
        //{
        //    if (IsStreaming)
        //    {
        //        Stop_Streaming();
        //        Thread.Sleep(10);
        //        USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Disable_AutoStart).ToArray(), 2, CurrentDeviceAddress);
        //        Thread.Sleep(10);
        //        Start_Streaming();
        //    }
        //    else
        //    {
        //        USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Disable_AutoStart).ToArray(), 2, CurrentDeviceAddress);
        //    }
        //}

        private void FlyoutAmpSetting_OnIsOpenChanged(object sender, RoutedEventArgs e)
        {
            //Todo need a deepcopy for this.
            if (FlyoutAmpSetting.IsOpen)
            {
                // Open
                if (INTAN[CurrentDeviceAddress] != null)
                    cfgSnapShot = INTAN[CurrentDeviceAddress].Configuration;
                else
                {
                    MessageBox.Show("No device connected!");
                    FlyoutAmpSetting.IsOpen = false;
                }
            }
            else
            {
                // Close
                var flyout = sender as Flyout;
                if (flyout != null && INTAN[CurrentDeviceAddress]!=null)
                {
                    var newCfg = flyout.Content as AmplifierSettings;
                    if (newCfg != null && !newCfg.CfgSend)
                        INTAN[CurrentDeviceAddress].Configuration = cfgSnapShot;
                }
            }
        }

        private void WifiTESTButton(object sender, RoutedEventArgs e)
        {
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");
            //var cmd = new byte[2];
            //const double max_spi_rate = 20000000;
            //var spi_freq = 10000000;

            //byte fx3ClkDiv = (byte) (max_spi_rate / spi_freq);

            //cmd[0] = (byte) (CC3200.CC3200Cmd.Change_SPI_Freq);
            //BitConverter.GetBytes(fx3ClkDiv).CopyTo(cmd, 1);
            USBridge_Wifi.cfg_send(CC3200.Target.CC3200, BitConverter.GetBytes((ushort)CC3200.CC3200Cmd.Send_via_SPI), 1, 0);
            //MessageBox.Show(USBridge_Wifi.IsAgentConnected().ToString());
        }

        private void SetRecMode(bool isLoggerModeChanged = false)
        {
            bool wasStreaming = false;
            bool wasLogging = false;

            if (IsStreaming)
            {
                Stop_Streaming();
                Thread.Sleep(10);
                wasStreaming = true;
            }

#if MultiAddr
                for (int addr = 0; addr < 32; addr++)
                {
                    if (INTAN[CurrentDeviceAddress].Connected && (FX3_IGLOO_Nano.Status)USBridge.DeviceStatus == FX3_IGLOO_Nano.Status.ReadOut)
                    {
                        USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle, addr);     // Exit readout mode
                    }
                }
#else
            if (INTAN[CurrentDeviceAddress].Connected && (FX3_IGLOO_Nano.Status)USBridge.DeviceStatus == FX3_IGLOO_Nano.Status.ReadOut)
            {
                USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Idle, CurrentDeviceAddress);     // Exit readout mode
            }
#endif


            if (IsLogging)
            {
                // During logging

                // If logger mode changed, stop logging first
                if (isLoggerModeChanged) USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Stop_Logging), 2, CurrentDeviceAddress);

                // Change both PC and logger recording mode
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                    BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Change_Mode |
                                            Convert.ToUInt16(Logger_RecMode.SelectedValue) |
                                            Convert.ToUInt16(PC_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
                // Restart logging
                if (isLoggerModeChanged) USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Start_Logging), 2, CurrentDeviceAddress);
            }
            else
            {
                // Set only primarly link
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                    BitConverter.GetBytes((ushort) FX3_IGLOO_Nano.IGLOO_CMDS.Change_Mode |
                                            Convert.ToUInt16(PC_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
            }

            // reset output stream so output files can be recreated
#if MultiAddr
                for (var addr = 0; addr < 32; addr++)
                    for (var channelId = 0; channelId < 32; channelId++)
                    {

                        if (OutputWriter[addr, channelId] != null)
                        {
                            OutputWriter[addr, channelId] = null;
                        }
                    }
#else
            if (wasStreaming)
            {
                for (var channelId = 0; channelId < 32; channelId++)
                {
                    OutputWriter[CurrentDeviceAddress, channelId] = null;
                    OutputSpikeWriter[CurrentDeviceAddress, channelId] = null;
                }
                // Restart streaming
                Start_Streaming();
            }
#endif
        }

        private object pc_old_rec_mode, logger_old_rec_mode;
        private void ComputerRecModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pc_old_rec_mode == e.AddedItems[0])
                return;
            pc_old_rec_mode = e.AddedItems[0];
            SetRecMode();
            if ((int) PC_RecMode.SelectedValue == (int) FX3_IGLOO_Nano.Mask_Set_Mode.En_PC_Spike)
                IsSpikeSorting = true;
            else
            {
                IsSpikeSorting = false;
            }
        }

        private void LoggerRecModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (logger_old_rec_mode == e.AddedItems[0])
                return;
            logger_old_rec_mode = e.AddedItems[0];
            SetRecMode(true);
        }

        private void Toggle_Logger(object sender, RoutedEventArgs e)
        {
            bool wasStreaming = false;
            IsLogging = !IsLogging;

            if (IsStreaming)
            {
                Stop_Streaming();
                wasStreaming = true;
            }

            if (IsAutoStart)
            {
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Enable_AutoStart | Convert.ToUInt16(Logger_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
            }
            else
            {
                // Make sure the autostart file is deleted
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Disable_AutoStart | Convert.ToUInt16(Logger_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
            }
            Thread.Sleep(10);

            if (IsLogging)
            {
                // Enable secondary link on FPGA first
                // Change both PC and logger recording mode
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                    BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Change_Mode |
                                            Convert.ToUInt16(Logger_RecMode.SelectedValue) |
                                            Convert.ToUInt16(PC_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
                // Creating the file on the logger
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Start_Logging), 2, CurrentDeviceAddress);
            }
            else
            {
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.Logger, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.Logger_CMDS.Stop_Logging), 2, CurrentDeviceAddress);
                // Disable secondary link on FPGA
                // Set only primarly link
                USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO,
                    BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Change_Mode |
                                            Convert.ToUInt16(PC_RecMode.SelectedValue)), 2, CurrentDeviceAddress);
            }

            if (wasStreaming)
            {
                for (var channelId = 0; channelId < 32; channelId++)
                {
                    OutputWriter[CurrentDeviceAddress, channelId] = null;
                    OutputSpikeWriter[CurrentDeviceAddress, channelId] = null;
                }
                Start_Streaming();
            }
            
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {

            Version version;
            Initialise();

            USBridge.DeviceAttached += USBridge_DeviceAttached;
            USBridge.DeviceRemoved += USBridge_DeviceRemoved;

            USBridge_Wifi.DeviceAttached += USBridge_Wifi_DeviceAttached;
            USBridge_Wifi.DeviceRemoved += USBridge_Wifi_DeviceRemoved;

            try
            {
                version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                version = Assembly.GetExecutingAssembly().GetName().Version;
            }

            VersionNumber.Content = string.Format(CultureInfo.InvariantCulture, @"{0}.{1}.{2} (r{3})", version.Major, version.Minor, version.Build, version.Revision);

            ProjectFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FileSaveMode = FileSaveModeList.PerAddress;

            RefreshControls();

            if (!IsStreaming) ConfigAndStart();

            //PC_RecMode.SelectedIndex = 0;
            //Logger_RecMode.SelectedIndex = 0;
            PC_RecMode.SelectionChanged += ComputerRecModeChanged;
            Logger_RecMode.SelectionChanged += LoggerRecModeChanged;
        }
    }



    public class MultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //string one = values[0] as string;
            //string two = values[1] as string;
            //string three = values[2] as string;
            //if(!string.IsNullOrEmpty(one) && !string.IsNullOrEmpty(two) && !string.IsNullOrEmpty(three))
            //{
            //    return one + two + three;
            //}
            //return null;
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}