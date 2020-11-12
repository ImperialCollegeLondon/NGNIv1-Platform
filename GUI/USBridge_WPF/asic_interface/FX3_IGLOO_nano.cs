using System;
using System.Linq;

namespace ASIC_Interface
{

    /// <summary>
    /// Interface implemented by FX3 + IGLOOnano
    /// derived from the FX3 interface
    /// </summary>
    public class FX3_IGLOO_Nano : FX3, IASIC_Interface
    {

        /// <summary>
        /// IGLOOnano operating mode (internal FSM)
        /// </summary>
         
        public new enum Target
        {
            FX3,
            IGLOO,
            ASIC,
            Logger
        }

        public new enum Status
        {
            Disconnected,
            Idle,
            Store_Template,
            Store_Tmp_Thres,
            Store_Spd_Thres,
            Pass_To_ASIC,
            ReadOut
        }

        //bool _tempalteMatching;

        /// <summary>
        /// Stages of configure devices
        /// </summary>
        public enum CfgStage : byte { Idle = 1, Set_Template, Set_Template_Thres, Set_Detec_Thres };


        #region Commands Definition

        public enum IGLOO_CMDS : ushort
        {
            // USB sending sequency is Little Endian
            // however FX3 will change it back to big endian before sending to FPGA
            Change_Mode = 0x0A01,
            Store_Template = 0x0A02,
            Store_Tmp_Thres = 0x0A04,
            Store_Spd_Thres = 0x0A08,
            Start_Read_Out = 0x0A10,
            Soft_Reset = 0x0A20,
            Pass_To_ASIC = 0x0A40,
            Change_LSB = 0x0A80,
            Query_Status = 0x0AFF,
            Stop = 0x8000,
            Null = 0x0000
        }

        public enum Logger_CMDS : ushort
        {
            // USB sending sequency is Little Endian
            // however FX3 will change it back to big endian before sending to FPGA
            // The commond will pass through FPGA, so must not clash with any IGLOO_CMDS.
            // No reponse from Logger will be received.
            Set_DeviceTime = 0x0C00,
            Start_Logging = 0x0C01,
            Stop_Logging = 0x0C02,
            Enable_AutoStart = 0x0C04,
            Disable_AutoStart = 0x0C08
        }

        #endregion Commands Definition

        #region Configuration mask
        // Last 5 bit is usually commands
        public const ushort MASK_CMD = 0x0AFF;
        // General data mask. Determined by IGLOO 9-bit data structure.
        public const ushort MASK_DATA = 0x01FF;
        // If the MSB is 1, then the data is invalid. Although in spike sorting mode, it carries a time-stamp.
        public const ushort INVALID_DATA_MASK = 0x8000;
        // Mask sets for different command
        public enum Mask_Set_Tmp : uint { Template = 0xC000, Channel = 0x3E00, Sample = 0x01E0, Cmd = MASK_CMD, Value = MASK_DATA }
        public enum Mask_Set_Tmp_Th : uint { Template = 0x0C00, Channel = 0x03E0, Cmd = MASK_CMD, Value = 0xFFFF }
        public enum Mask_Set_Spd_Th : uint { Channel = 0x03E0, Cmd = MASK_CMD, Value = MASK_DATA }
        public enum Mask_Set_LSB : uint { LSBbit = 0x7000 }
        public enum Mask_Set_Mode : uint { En_Logger_Sig = 0x1000, En_Logger_Spike = 0x2000, En_PC_Sig = 0x4000, En_PC_Spike = 0x8000}


#endregion

        #region Data mask
        public enum SpikeSignalMask : ushort
        {
            Spike_Time_Mask = 0x001F,
            Channel_Mask = 0x03E0,
            Template_Mask = 0x0C00,
            TimeTick = 0xC000
        };

        public enum RawSignalMask : ushort
        {
            Channel_Mask = 0x3E00,
            Value_Mask = MASK_DATA
        }
        #endregion
        
        //// IGLOO address
        //private readonly int _addr;
        //public int Addr { get { return _addr; } }

        private Status _deviceStatus;
        Enum IASIC_Interface.DeviceStatus { get { return _deviceStatus; } }


        /// <summary>
        /// Switches the mode of the FPGA
        /// </summary>
        /// <param name="target">The target device</param>
        /// <param name="targeMode">The target mode</param>
        /// <param name="deviceAddr">The target device address</param>
        void IASIC_Interface.SwitchMode(Enum target, Enum targeMode, int deviceAddr)
        {
            Status targetMode = (Status)targeMode;
            Target targetDevice = (Target)target;

            if (targetDevice == Target.IGLOO && _deviceStatus != targetMode)
            {
                switch (targetMode)
                {
                    case Status.Disconnected:
                        _deviceStatus = targetMode;
                        break;
                    case Status.Idle:
                        // Change on 20/07/2015, Split toggle command into 2 separate commands
                        if (_deviceStatus != Status.ReadOut)
                        {
                            // Send stop command
                            cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop), 2, deviceAddr);
                        }
                        // The stop command is not send by firmware on FX3 to exit readout because it needs to access the CS in readout mode
                        _deviceStatus = targetMode;
                        break;
                    case Status.Store_Template:
                        cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop).Concat(BitConverter.GetBytes((ushort)IGLOO_CMDS.Store_Template)).ToArray(), 4, deviceAddr);
                        _deviceStatus = targetMode;
                        break;
                    case Status.Store_Tmp_Thres:
                        cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop).Concat(BitConverter.GetBytes((ushort)IGLOO_CMDS.Store_Tmp_Thres)).ToArray(), 4, deviceAddr);
                        _deviceStatus = targetMode;
                        break;
                    case Status.Store_Spd_Thres:
                        cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop).Concat(BitConverter.GetBytes((ushort)IGLOO_CMDS.Store_Spd_Thres)).ToArray(), 4, deviceAddr);
                        _deviceStatus = targetMode;
                        break;
                    case Status.Pass_To_ASIC:
                        cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop).Concat(BitConverter.GetBytes((ushort)IGLOO_CMDS.Pass_To_ASIC)).ToArray(), 4, deviceAddr);
                        _deviceStatus = targetMode;
                        break;
                    case Status.ReadOut:
                        cfg_send(FX3.Target.Children, BitConverter.GetBytes((ushort)IGLOO_CMDS.Stop).Concat(BitConverter.GetBytes((ushort)IGLOO_CMDS.Start_Read_Out)).ToArray(), 4, deviceAddr);
                        _deviceStatus = targetMode;
                        break;
                }
            }
            else if (targetDevice == Target.FX3)
            {
                base.SwitchMode(FX3.Target.FX3, targeMode, deviceAddr);    //Need to translate the targetdevice to the enum of FX3 class.
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="FX3_IGLOO_Nano"/> class.
        /// </summary>
        /// <param name="vid">The vid.</param>
        /// <param name="pid">The pid.</param>
        public FX3_IGLOO_Nano(int vid, int pid) : base(vid, pid) { 
            //_tempalteMatching = false;

            _deviceStatus = (FX3.Status)base.DeviceStatus == FX3.Status.Disconnected ? Status.Disconnected : Status.Idle;

            base.DeviceAttached += FX3_IGLOOnano_DeviceAttached;
            base.DeviceRemoved += FX3_IGLOOnano_DeviceRemoved;
        }

        // Implement this class event handeling methods
        private void FX3_IGLOOnano_DeviceRemoved(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if ((FX3.Status)base.DeviceStatus == FX3.Status.Disconnected && _deviceStatus != Status.Disconnected)
                _deviceStatus = Status.Disconnected;
            OnDeviceRemoved(new FX3_IGLOO_Nano_EventArgs());
        }
        private void FX3_IGLOOnano_DeviceAttached(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (((FX3.Status)base.DeviceStatus != FX3.Status.Disconnected) && (_deviceStatus == Status.Disconnected))
                _deviceStatus = Status.Idle;
            OnDeviceAttached(new FX3_IGLOO_Nano_EventArgs());

        }

        protected virtual void OnDeviceAttached(FX3_IGLOO_Nano_EventArgs e)
        {
            if (DeviceAttached != null) DeviceAttached(this, e);
        }

        protected virtual void OnDeviceRemoved(FX3_IGLOO_Nano_EventArgs e)
        {
            if (DeviceRemoved != null) DeviceRemoved(this, e);
        }

        public class FX3_IGLOO_Nano_EventArgs : EventArgs {}

        // Create events for Interfaces
        new event EventHandler DeviceAttached;
        new event EventHandler DeviceRemoved;

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


        /// <summary>
        /// Send the configuration via FPGA
        /// </summary>
        /// <param name="target"></param>
        /// <param name="content">The content.</param>
        /// <param name="length">The length.</param>
        /// <param name="channelAddr"></param>
        void IASIC_Interface.cfg_send(Enum target, byte[] content, int length, int channelAddr)
        {
            var targetDevice = (Target)target;
            switch(targetDevice)
            {
                case Target.ASIC:
                case Target.IGLOO:
                case Target.Logger:
                    cfg_send(FX3.Target.Children, content, length, channelAddr);
                    break;
                case Target.FX3:
                    cfg_send(FX3.Target.FX3, content, length, channelAddr);
                    break;
            }
        }


        /// <summary>
        /// Receive info via FPGA
        /// </summary>
        /// <param name="target"></param>
        /// <param name="content">The content.</param>
        /// <param name="length">The length.</param>
        /// <param name="channelAddr"></param>
        void IASIC_Interface.cfg_receive(Enum target, ref byte[] content, int length, int channelAddr)
        {
            var targetDevice = (Target)target;
            switch (targetDevice)
            {
                case Target.ASIC:
                case Target.IGLOO:
                    cfg_receive(FX3.Target.Children, ref content, length, channelAddr);
                    break;
                case Target.FX3:
                    cfg_receive(FX3.Target.FX3, ref content, length, channelAddr);
                    break;
            }
        }

    }
}
