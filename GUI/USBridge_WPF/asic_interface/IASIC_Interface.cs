using System;
using System.Threading.Tasks.Dataflow;

namespace ASIC_Interface
{
    /// <summary>
    /// Communication interface between computer and ASIC
    /// Physically implemented by Logger/FPGA
    /// Underlying structure can be rewritten to provide the link
    /// </summary>
    public interface IASIC_Interface : IDisposable
    {

        #region Property

        /// <summary>
        /// Check if the interface is running in the selected mode
        /// </summary>
        
        Enum DeviceStatus { get; }

        string FriendlyName { get; }

        ulong InputDataLength { get; }
        ulong OutputDataLength { get; }



        #endregion Property

        #region Methods
        /// <summary>
        /// Connect to the interface
        /// </summary>
        /// <returns></returns>
        void Connect();

        /// <summary>
        /// Dispose the interface
        /// </summary>
        new void Dispose();

        /// <summary>
        /// Initialise the interface to default settings
        /// </summary>
        void Init();

        void Deinit();

        /// <summary>
        /// Change the interface mode
        /// </summary>
        /// <param name="target">Target Device</param>
        /// <param name="targeMode">Default to Configure mode</param>
        /// <param name="deviceAddr">Device Address</param>
        void SwitchMode(Enum target, Enum targeMode, int deviceAddr);

        /// <summary>
        /// Reset interface
        /// </summary>
        void Reset();

        /// <summary>
        /// ASIC connection DeviceStatus
        /// </summary>
        bool IsASICConnected(int addr);

        /// <summary>
        /// Agent Device connection status
        /// </summary>
        bool IsAgentConnected();
        #endregion Methods

        #region Events

        /// <summary>
        /// Occurs when [device attached].
        /// </summary>
        event EventHandler DeviceAttached;


        /// <summary>
        /// Occurs when [device removed].
        /// </summary>
        event EventHandler DeviceRemoved;

        #endregion Events

        #region DataFlow Methods

        /// <summary>
        /// Send data via the interface. 
        /// Make sure the device knows how to interpret the data.
        /// </summary>
        /// <param name="targetDevice"></param>
        /// <param name="content"></param>
        /// <param name="length">in number of bytes</param>
        /// <param name="channelAddr">addresses. Default to 0</param>
        void cfg_send(Enum @targetDevice, byte[] content, int length, int channelAddr);

        /// <summary>
        /// Receive data via the interface
        /// </summary>
        /// <param name="targetDevice"></param>
        /// <param name="content"></param>
        /// <param name="length"></param>
        /// <param name="channelAddr">addresses. Default to 0</param>
        void cfg_receive(Enum @targetDevice, ref byte[] content, int length, int channelAddr);


        /// <summary>
        /// Initializes the input data flow.
        /// </summary>
        /// <param name="targetDataflow">The _target dataflow.</param>
        /// <param name="queueSz">Queue size.</param>
        /// <param name="ppx">packets per transfer.</param>
        void InitInputDataFlow(ITargetBlock<byte[]> targetDataflow, int queueSz, int ppx);


        /// <summary>
        /// Starts the input data flow.
        /// </summary>
        void StartInputDataFlow();

        /// <summary>
        /// Stops the input data flow.
        /// </summary>
        void StopInputDataFlow();


        /// <summary>
        /// Initializes the output data flow.
        /// </summary>
        void InitOutputDataFlow(ITargetBlock<byte[]> targetDataflow);


        /// <summary>
        /// Starts the output data flow.
        /// </summary>
        void StartOutputDataFlow();

        /// <summary>
        /// Stops the output data flow.
        /// </summary>
        void StopOutputDataFlow();

        #endregion  DataFlow Methods

    }
}
