using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using CyUSB;

// Distributed by NuGet Packages


namespace ASIC_Interface
{
    /// <summary>
    /// Interaface implemented by FX3
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class FX3 : IASIC_Interface
    {
        protected static int MaxEp0Length = 4096;
        public enum Target
        {
            Children,
            FX3
        };

        public enum Status
        {
            Disconnected,
            Configure,
            StreamIn,
            StreamOut
        }

        USBDeviceList _usbDevices;

        public CyUSBDevice Device { get; set; }
        CyUSBEndPoint _inEpt, _outEpt;
        static CyControlEndPoint _ctrlEpt;

        /* device code define */
        readonly int _vid;
        readonly int _pid;

        Status _deviceStatus;

        public enum FX3Cmd : ushort
        {
            Set_Mode_Readout = 0x01,
            Set_Mode_Config = 0x10,
            Change_Pib_Freq = 0x0A
        }

        /* default sending data */
        readonly byte[] _asicTag = new byte[] { 0xF, 0x3 };

        /* transfer variables */
        int _bufSz;
        int _queueSz;
        int _ppx;
        int _isoPktBlockSize;
        double _xferBytes;
        int _successes;
        int _failures;

        /* Thread USB Input Stream */
        //Thread thread_StreamIn, thread_OutputStream;
        static bool _isLiveInputStream;
        static bool _isLiveOutputStream;

        //public Stream inputStream;

        //public Stream outputStream;

        //public Stream syncInputStream;

        public ITargetBlock<byte[]> InputDataFlow, OutputDataFlow;

        // These are needed to close the app from the Thread exception(exception handling)
        delegate void ExceptionCallback();

        readonly ExceptionCallback _handleException;

        // BufferFiller
        private const byte DefaultBufInitValue = 0xF5;

        ulong _inputDataLength, _outputDataLength;

        /* firmware command define */
        //public static byte DEVICE_READOUT_MODE = 0x01;
        //public static byte DEVICE_CONFIG_MODE = 0x10;
        public static byte[] SPI_TOGGLE_MODE = { 0x00, 0x01 };
        public static byte[] SPI_STORE_TMP = { 0x00, 0x02 };
        public static byte[] SPI_STORE_TMP_THRESH = { 0x00, 0x04 };
        public static byte[] SPI_STORE_SPD_THRESH = { 0x00, 0x08 };
        public static byte[] SPI_READ_OUT = { 0x00, 0x10 };
        public static byte[] SPI_STOP = { 0x80, 0x00 };

        public static int SPI_RESET = 32;
        public static int DEVICE_CMD = 64;

        /* Endpoint addresses */
        byte InEpt_addr = 0x83;
        byte OutEpt_addr = 0x03;

        public FX3(int vid, int pid)
        {
            _pid = pid;
            _vid = vid;

            _handleException = ThreadException;

            //CyConst.SetClassGuid("36FC9E60-C465-11CF-8056-444553540000");

            _usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);
            _usbDevices.DeviceAttached += FX3_DeviceAttached;
            _usbDevices.DeviceRemoved += FX3_DeviceRemoved;

            Connect();
            if (IsConnected)
                Init();
        }

        // Implement this class event handeling methods
        private void FX3_DeviceRemoved(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (IsConnected)
            {
                // Check if it is still there;
                Connect();
                if (!IsConnected)
                {
                    Deinit();
                    OnDeviceRemoved(new FX3_EventArgs());
                    //this.DeviceRemoved(sender, e);
                }
            }
        }
        private void FX3_DeviceAttached(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (!IsConnected)
            {
                //Try connection
                Connect();
                if (IsConnected)
                {
                    Init();
                    OnDeviceAttached(new FX3_EventArgs());
                    //this.DeviceAttached(sender, e);
                }
            }
        }
        protected virtual void OnDeviceAttached(FX3_EventArgs e)
        {
            if (DeviceAttached != null) DeviceAttached(this, e);
        }

        protected virtual void OnDeviceRemoved(FX3_EventArgs e)
        {
            if (DeviceRemoved != null) DeviceRemoved(this, e);
        }

        public class FX3_EventArgs : EventArgs { }

        // Create events for Interfaces
        public bool IsAgentConnected()
        {
            //throw new NotImplementedException();
            return IsConnected;
        }

        public event EventHandler DeviceAttached;
        public event EventHandler DeviceRemoved;

        // Associate interface events with this class defined events
        event EventHandler IASIC_Interface.DeviceAttached
        {
            add
            {
                DeviceAttached += value;
            }
            remove
            {
                DeviceAttached -= value;
            }
        }
        event EventHandler IASIC_Interface.DeviceRemoved
        {
            add
            {
                DeviceRemoved += value;
            }
            remove
            {
                DeviceRemoved -= value;
            }
        }


        public void Connect()
        {
            //throw new NotImplementedException();
            if (_usbDevices.Count > 0)
                Device = _usbDevices[_vid, _pid] as CyUSBDevice;
            else
                Device = null;
        }

        public void Init()
        {
            //throw new NotImplementedException();
            _ctrlEpt = Device.ControlEndPt;
            _inEpt = Device.EndPointOf(InEpt_addr);
            _outEpt = Device.EndPointOf(OutEpt_addr);
            _deviceStatus = Status.Configure;
        }

        public void SwitchMode(Enum target, Enum targeMode, int deviceAddr)
        {
            var targetDevice = (Target)target;
            var mode = (Status)targeMode;
            if (targetDevice != Target.FX3) return;
            _deviceStatus = mode;
            switch (_deviceStatus)
            {
                case Status.Configure:
                    cfg_send(Target.FX3, BitConverter.GetBytes((ushort)FX3Cmd.Set_Mode_Config), 1, deviceAddr);
                    break;
                case Status.StreamIn:
                    cfg_send(Target.FX3, BitConverter.GetBytes((ushort)FX3Cmd.Set_Mode_Readout), 1, deviceAddr);
                    break;
            }
            // FX3 won't know the targeMode supported by the ASIC. this method needs override.
        }

        public void Reset()
        {
            //throw new NotImplementedException();
            if (IsConnected)
            {
                //Device.Reset();
                Device.ReConnect(); // Recircle power
                _deviceStatus = Status.Configure;
            }
        }

        public void cfg_send(Enum targetDevice, byte[] content, int length, int channelAddr)
        {
            if (_ctrlEpt != null)
            {
                Target type = (Target)targetDevice;
                _ctrlEpt.Target = CyConst.TGT_DEVICE;
                _ctrlEpt.ReqType = CyConst.REQ_VENDOR;
                _ctrlEpt.Value = 0;
                _ctrlEpt.Index = (ushort)(channelAddr);

                byte[] partialContent = new byte[MaxEp0Length];
                int sendPosition = 0;

                switch (type)
                {
                    case Target.FX3:
                        _ctrlEpt.ReqCode = 0x10;
                        break;
                    case Target.Children:
                        _ctrlEpt.ReqCode = 0x00;
                        break;
                }


                try
                {
                    if (length > MaxEp0Length)
                    {
                        while (length > MaxEp0Length)
                        {
                            Array.Copy(content, sendPosition, partialContent, 0, MaxEp0Length);
                            _ctrlEpt.Write(ref partialContent, ref MaxEp0Length);
                            length -= MaxEp0Length;
                            sendPosition += MaxEp0Length;
                        }

                        Array.Copy(content, sendPosition, partialContent, 0, length);
                        _ctrlEpt.Write(ref partialContent, ref length);
                    }
                    else
                    {
                        _ctrlEpt.Write(ref content, ref length);
                    }
                }
                catch
                {
                    Console.Error.WriteLine("Error sending via USB control endpoint.");
                }
            }
        }

        public void cfg_receive(Enum targetDevice, ref byte[] content, int length, int channelAddr)
        {
            // exit if _ctrlEpt hasn't been set
             if (_ctrlEpt == null)
                return;



            Target type = (Target)targetDevice;
            _ctrlEpt.Target = CyConst.TGT_DEVICE;
            _ctrlEpt.ReqType = CyConst.REQ_VENDOR;
            _ctrlEpt.Value = 0;
            _ctrlEpt.Index = (ushort)(channelAddr);

            switch (type)
            {
                case Target.FX3:
                    _ctrlEpt.ReqCode = 0x11;
                    break;
                case Target.Children:
                    _ctrlEpt.ReqCode = 0x01;
                    break;
            }

            try
            {
                _ctrlEpt.Read(ref content, ref length);
            }
            catch
            {
                Console.Error.WriteLine("Error sending via USB control endpoint.");
            }
        }


        /// <summary>
        /// Interface connection DeviceStatus
        /// </summary>
        public bool IsConnected
        {
            get
            {
                //throw new NotImplementedException(); 
                return (Device != null);
            }
        }

        public bool IsASICConnected(int addr)
        {
            //throw new NotImplementedException();
            var identification = new byte[2];
            cfg_receive(Target.Children, ref identification, 2, 0);

            return (identification == _asicTag);
        }



        public void Dispose()
        {
            //throw new NotImplementedException();
            Deinit();

            if (_usbDevices != null)
                _usbDevices.Dispose();
        }

        public void initOutputDataFlow()
        {
            throw new NotImplementedException();
        }

        # region Dataflow

        /// <summary>
        /// Initializes the input stream.
        /// </summary>
        /// <param name="targetDataflow">The _target dataflow.</param>
        /// <param name="queueSz">The _ queue sz.</param>
        /// <param name="ppx">The _ PPX.</param>
        public void InitInputDataFlow(ITargetBlock<byte[]> targetDataflow, int queueSz, int ppx)
        {
            //throw new NotImplementedException();


            if (_deviceStatus != Status.StreamIn) return;
            // For SSD
            //QueueSz = 128;
            //PPX = 32;

            // For SATA
            //QueueSz = 32;
            //PPX = 8;

            // Hook up the dataflow.
            InputDataFlow = targetDataflow;

            _queueSz = queueSz;
            _ppx = ppx;

            _bufSz = _inEpt.MaxPktSize * _ppx;

            _inEpt.XferSize = _bufSz;

            var ept = _inEpt as CyIsocEndPoint;
            _isoPktBlockSize = ept != null ? ept.GetPktBlockSize(_bufSz) : 0;

            _isLiveInputStream = true;

            //inputStream = new MemoryStream();
            ////inputStream.Flush();
            ////inputStream.Position = 0;
            //syncInputStream = Stream.Synchronized(inputStream);
            //syncInputStream.Flush();
            //syncInputStream.Position = 0;

            //thread_StreamIn = new Thread(new ThreadStart(InputThread));
            //thread_StreamIn.IsBackground = true;
            //thread_StreamIn.Priority = ThreadPriority.Highest;
            //thread_StreamIn.Start();
        }



        /// <summary>
        /// Data Xfer Thread entry point.
        /// </summary>
        /// 
        public void InputThread()
        {
            // Setup the queue buffers
            byte[][] cmdBufs = new byte[_queueSz][];
            byte[][] xferBufs = new byte[_queueSz][];
            byte[][] ovLaps = new byte[_queueSz][];
            ISO_PKT_INFO[][] pktsInfo = new ISO_PKT_INFO[_queueSz][];

            _inputDataLength = 0;
            //int xStart = 0;

            //////////////////////////////////////////////////////////////////////////////
            ///////////////Pin the data buffer memory, so GC won't touch the memory///////
            //////////////////////////////////////////////////////////////////////////////

            GCHandle cmdBufferHandle = GCHandle.Alloc(cmdBufs[0], GCHandleType.Pinned);
            GCHandle xFerBufferHandle = GCHandle.Alloc(xferBufs[0], GCHandleType.Pinned);
            GCHandle overlapDataHandle = GCHandle.Alloc(ovLaps[0], GCHandleType.Pinned);
            GCHandle pktsInfoHandle = GCHandle.Alloc(pktsInfo[0], GCHandleType.Pinned);

            try
            {
                LockNLoad(cmdBufs, xferBufs, ovLaps, pktsInfo);
            }
            catch (NullReferenceException e)
            {
                // This exception gets thrown if the device is unplugged 
                // while we're streaming data
                e.GetBaseException();
                _handleException();
            }

            //////////////////////////////////////////////////////////////////////////////
            ///////////////Release the pinned memory and make it available to GC./////////
            //////////////////////////////////////////////////////////////////////////////
            cmdBufferHandle.Free();
            xFerBufferHandle.Free();
            overlapDataHandle.Free();
            pktsInfoHandle.Free();

            InputDataFlow.Complete();
        }

        /// <summary>
        /// This is a recursive routine for pinning all the buffers used in the transfer in memory.
        /// It will get recursively called QueueSz times.  On the QueueSz_th call, it will call
        /// XferData, which will loop, transferring data, until the stop button is clicked.
        /// Then, the recursion will unwind.
        /// </summary>
        /// <param name="cBufs">The c bufs.</param>
        /// <param name="xBufs">The x bufs.</param>
        /// <param name="oLaps">The o laps.</param>
        /// <param name="pktsInfo">The PKTS information.</param>
        public unsafe void LockNLoad(byte[][] cBufs, byte[][] xBufs, byte[][] oLaps, ISO_PKT_INFO[][] pktsInfo)
        {
            int j = 0;

            GCHandle[] bufSingleTransfer = new GCHandle[_queueSz];
            GCHandle[] bufDataAllocation = new GCHandle[_queueSz];
            GCHandle[] bufPktsInfo = new GCHandle[_queueSz];
            GCHandle[] handleOverlap = new GCHandle[_queueSz];

            while (j < _queueSz)
            {
                // Allocate one set of buffers for the queue, Buffered IO method require user to allocate a buffer as a part of command buffer,
                // the BeginDataXfer does not allocated it. BeginDataXfer will copy the data from the main buffer to the allocated while initializing the commands.
                cBufs[j] = new byte[CyConst.SINGLE_XFER_LEN + _isoPktBlockSize + ((_inEpt.XferMode == XMODE.BUFFERED) ? _bufSz : 0)];

                xBufs[j] = new byte[_bufSz];

                //initialize the buffer with initial value 0xA5
                for (int iIndex = 0; iIndex < _bufSz; iIndex++)
                    xBufs[j][iIndex] = DefaultBufInitValue;

                int sz = Math.Max(CyConst.OverlapSignalAllocSize, sizeof(OVERLAPPED));
                oLaps[j] = new byte[sz];
                pktsInfo[j] = new ISO_PKT_INFO[_ppx];

                /*/////////////////////////////////////////////////////////////////////////////
                 * 
                 * fixed keyword is getting thrown own by the compiler because the temporary variables 
                 * tL0, tc0 and tb0 aren't used. And for jagged C# array there is no way, we can use this 
                 * temporary variable.
                 * 
                 * Solution  for Variable Pinning:
                 * Its expected that application pin memory before passing the variable address to the
                 * library and subsequently to the windows driver.
                 * 
                 * Cypress Windows Driver is using this very same memory location for data reception or
                 * data delivery to the device.
                 * And, hence .Net Garbage collector isn't expected to move the memory location. And,
                 * Pinning the memory location is essential. And, not through FIXED keyword, because of 
                 * non-usability of temporary variable.
                 * 
                /////////////////////////////////////////////////////////////////////////////*/
                //fixed (byte* tL0 = oLaps[j], tc0 = cBufs[j], tb0 = xBufs[j])  // Pin the buffers in memory
                //////////////////////////////////////////////////////////////////////////////////////////////
                bufSingleTransfer[j] = GCHandle.Alloc(cBufs[j], GCHandleType.Pinned);
                bufDataAllocation[j] = GCHandle.Alloc(xBufs[j], GCHandleType.Pinned);
                bufPktsInfo[j] = GCHandle.Alloc(pktsInfo[j], GCHandleType.Pinned);
                handleOverlap[j] = GCHandle.Alloc(oLaps[j], GCHandleType.Pinned);
                // oLaps "fixed" keyword variable is in use. So, we are good.
                /////////////////////////////////////////////////////////////////////////////////////////////            

                //fixed (byte* tL0 = oLaps[j])
                {
                    var ovLapStatus = (OVERLAPPED)Marshal.PtrToStructure(handleOverlap[j].AddrOfPinnedObject(), typeof(OVERLAPPED));
                    ovLapStatus.hEvent = PInvoke.CreateEvent(0, 0, 0, 0);
                    Marshal.StructureToPtr(ovLapStatus, handleOverlap[j].AddrOfPinnedObject(), true);

                    // Pre-load the queue with a request
                    int len = _bufSz;
                    if (_inEpt.BeginDataXfer(ref cBufs[j], ref xBufs[j], ref len, ref oLaps[j]) == false)
                        _failures++;
                }
                j++;
            }

            XferData(cBufs, xBufs, oLaps, pktsInfo, handleOverlap);          // All loaded. Let's go!

            int nLocalCount;
            for (nLocalCount = 0; nLocalCount < _queueSz; nLocalCount++)
            {
                var ovLapStatus = (OVERLAPPED)Marshal.PtrToStructure(handleOverlap[nLocalCount].AddrOfPinnedObject(), typeof(OVERLAPPED));
                PInvoke.CloseHandle(ovLapStatus.hEvent);

                /*////////////////////////////////////////////////////////////////////////////////////////////
                     * 
                     * Release the pinned allocation handles.
                     * 
                    ////////////////////////////////////////////////////////////////////////////////////////////*/
                bufSingleTransfer[nLocalCount].Free();
                bufDataAllocation[nLocalCount].Free();
                bufPktsInfo[nLocalCount].Free();
                handleOverlap[nLocalCount].Free();

                cBufs[nLocalCount] = null;
                xBufs[nLocalCount] = null;
                oLaps[nLocalCount] = null;
            }
            GC.Collect();
        }

        /*Summary
          Called at the end of recursive method, LockNLoad().
          XferData() implements the infinite transfer loop
        */
        public void XferData(byte[][] cBufs, byte[][] xBufs, byte[][] oLaps, ISO_PKT_INFO[][] pktsInfo, GCHandle[] handleOverlap)
        {
            int k = 0;
            int len = 0;

            _successes = 0;
            _failures = 0;

            _xferBytes = 0;
            //t1 = DateTime.Now;

            for (; _isLiveInputStream && _inEpt != null;)
            {
                // WaitForXfer
                //fixed (byte* tmpOvlap = oLaps[k])
                {
                    var ovData = (OVERLAPPED)Marshal.PtrToStructure(handleOverlap[k].AddrOfPinnedObject(), typeof(OVERLAPPED));
                    if (_inEpt != null && !_inEpt.WaitForXfer(ovData.hEvent, 200))
                    {
                        if (_inEpt != null) _inEpt.Abort();
                        PInvoke.WaitForSingleObject(ovData.hEvent, 1);
                    }
                }

                if (_inEpt != null && _inEpt.Attributes == 1)
                {
                    var isoc = _inEpt as CyIsocEndPoint;
                    // FinishDataXfer
                    if (isoc != null && isoc.FinishDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k], ref pktsInfo[k]))
                    {
                        //XferBytes += len;
                        //Successes++;

                        ISO_PKT_INFO[] pkts = pktsInfo[k];

                        for (int j = 0; j < _ppx; j++)
                        {
                            if (pkts[j].Status == 0)
                            {
                                _xferBytes += pkts[j].Length;

                                _successes++;

                            }
                            else
                                _failures++;

                            pkts[j].Length = 0;
                        }
                        //Outputfile.Write(xBufs[k], 0, len);
                        //inputStream.WriteAsync(xBufs[k], 0, len);
                        //syncInputStream.Write(xBufs[k], 0, len);
                        //if (InputDataLength > 196608)
                        {
                            InputDataFlow.Post(xBufs[k]); //Skip the leftover data from the previous transfer
                        }

                        _inputDataLength += (ulong)xBufs[k].LongLength;
                    }
                    //else
                    _failures++;
                }
                else
                {
                    // FinishDataXfer
                    if (_inEpt != null && _inEpt.FinishDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]))
                    {
                        _xferBytes += len;
                        _successes++;
                        //Outputfile.Write(xBufs[k], 0, len);
                        //inputStream.WriteAsync(xBufs[k], 0, len);
                        //syncInputStream.Write(xBufs[k], 0, len);
                        //if (InputDataLength > 196608)
                        {
                            InputDataFlow.Post(xBufs[k]); //Skip the leftover data from the previous transfer
                        }
                        _inputDataLength += (ulong)xBufs[k].LongLength;

                    }
                    else
                        _failures++;
                }

                // Re-submit this buffer into the queue
                len = _bufSz;
                if (_inEpt != null && _inEpt.BeginDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]) == false)
                    _failures++;

                k++;
                if (k == _queueSz)  // Only update displayed stats once each time through the queue
                {
                    k = 0;

                    //t2 = DateTime.Now;
                    //elapsed = t2 - t1;

                    //xferRate = (long)(XferBytes / elapsed.TotalMilliseconds);
                    //xferRate = xferRate / (int)100 * (int)100;

                    //// Call StatusUpdate() in the main thread
                    //if (bRunning == true) this.Invoke(updateUI);

                    // For small QueueSz or PPX, the loop is too tight for UI thread to ever get service.   
                    // Without this, app hangs in those scenarios.
                    Thread.Sleep(0);
                }
                Thread.Sleep(0);

            } // End infinite loop
            // Let's recall all the queued buffer and abort the end point.
            if (_inEpt != null) _inEpt.Abort();
            //Outputfile.Close();
        }


        /*Summary
        The callback routine delegated to handleException.
        */
        public void ThreadException()
        {
            //isLive_InputStream = false;

            //t2 = DateTime.Now;
            //elapsed = t2 - t1;
            //xferRate = (long)(XferBytes / elapsed.TotalMilliseconds);
            //xferRate = xferRate / (int)100 * (int)100;

            //thread_StreamIn = null;
            InputDataFlow.Fault(new Exception("Unplugged while streaming from USB"));
            Console.Error.Write("Unplugged while streaming from USB");

            //Btn_SaveFile.Background = SystemColors;
        }

        public void StopInputDataFlow()
        {
            _isLiveInputStream = false;
        }

        public void StopOutputDataFlow()
        {
            throw new NotImplementedException();
        }
        #endregion Dataflow

        public void Deinit()
        {
            //throw new NotImplementedException();
            _ctrlEpt = null;
            _inEpt = null;
            _outEpt = null;
            _deviceStatus = Status.Disconnected;

        }


        public void InitOutputDataFlow(ITargetBlock<byte[]> targetDataflow)
        {
            throw new NotImplementedException();
        }


        public ulong InputDataLength
        {
            get { return _inputDataLength; }
        }

        public ulong OutputDataLength
        {
            get { return _outputDataLength; }
        }

        public string FriendlyName
        {
            get { return Device != null ? Device.FriendlyName : "No device connected."; }
        }

        public Enum DeviceStatus
        {
            get { return _deviceStatus; }
        }


        public void StartInputDataFlow()
        {
            //throw new NotImplementedException();
            InputThread();
        }

        public void StartOutputDataFlow()
        {
            throw new NotImplementedException();
        }
    }
}