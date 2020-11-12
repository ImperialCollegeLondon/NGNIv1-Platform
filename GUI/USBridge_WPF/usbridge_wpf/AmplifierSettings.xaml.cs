using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ASIC_Interface;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for AmplifierSettings.xaml
    /// </summary>
    public partial class AmplifierSettings
    {
        public bool CfgSend = false;

        public AmplifierSettings()
        {
            InitializeComponent();
        }

        private void sampling_Rate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            const double sysClk = 403200000;
            var cmd = new byte[4];
            var sr = (int)Sampling_Rate.SelectedValue;

            var temp = sysClk / (sr * 32 * 16 * 2);
            var fx3ClkDiv = (ushort)(Math.Floor(temp));
            var isHalf = (byte)(temp - fx3ClkDiv > 0.5 ? 1 : 0);

            // PIB frequency is no longer coupled to sampling frequcy.
            //cmd[0] = (byte)(FX3.FX3Cmd.Change_Pib_Freq);
            //BitConverter.GetBytes(fx3ClkDiv).CopyTo(cmd, 1);ls
            //cmd[2] = isHalf;

            //if ((FX3_IGLOO_Nano.Status)MainWindow.USBridge.DeviceStatus != FX3_IGLOO_Nano.Status.Disconnected)
            //{
            //    MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.FX3, cmd, cmd.Length, 0);
            //}

            // Synchronise all the addresses
            foreach (var valueDispPair in MainWindow.ActiveDeviceAddressList)
            {
                MainWindow.INTAN[valueDispPair.Value].SamplingRate = sr;
            }

        }

        private void CFG_INTAN_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.IsStreaming)
            { 
                MainWindow.AppMainWindow.Stop_Streaming();
                Thread.Sleep(10);
                UploadConfiguration();
                Thread.Sleep(10);
                MainWindow.AppMainWindow.Start_Streaming();
            }
            else
            {
                UploadConfiguration();
            }

            CfgSend = true;
        }

        public static void UploadConfiguration()
        {
            List<int> _intanCfg = new List<int>();
            byte[] _readback = new byte[0x1000];


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

            // Configure 16 to 9 ADC range mappping
            // This must be done before config ASIC to ensure MCU store the settings correctly.
            MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes(
                (ushort)
                ((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Change_LSB |
                 (ushort)(MainWindow.INTAN[MainWindow.CurrentDeviceAddress].LSBMapBit << 12))), 2, MainWindow.CurrentDeviceAddress);
            MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes(
                (ushort)(0x0000)), 2, MainWindow.CurrentDeviceAddress);

            MainWindow.USBridge.SwitchMode(FX3_IGLOO_Nano.Target.IGLOO, FX3_IGLOO_Nano.Status.Pass_To_ASIC, MainWindow.CurrentDeviceAddress);
            MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.ASIC, _intanCfg.SelectMany(i => BitConverter.GetBytes((ushort)(i & 0xFFFF))).ToArray(), 2 * _intanCfg.Count, MainWindow.CurrentDeviceAddress);
            //MainWindow.USBridge.cfg_send(FX3_IGLOO_Nano.Target.IGLOO, BitConverter.GetBytes((ushort)FX3_IGLOO_Nano.IGLOO_CMDS.Stop), 2, MainWindow.CurrentDeviceAddress);
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

            // For debugging communication
            MessageBox.Show(str.ToString());
        }

    }
}
