using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace ASIC_Interface
{
    public class CC3200 : IASIC_Interface
    {
        public enum Target
        {
            Children,
            CC3200
        }

        public enum Status
        {
            Disconnected,
            Configure,
            StreamIn,
            StreamOut
        }
        public enum CC3200Cmd : ushort
        {
            Set_Mode_Readout = 0x01,
            Set_Mode_Config = 0x10,
            Change_SPI_Freq = 0x0A,
            Send_Readback = 0x0F,
            Send_Beacon = 0x20,
            Send_via_SPI = 0x40
        }

        public enum CC3200Msg : byte
        {
            Connect = 0x01,
            Disconnect = 0x02,
            Beacon = 0x04
        }


        public Enum DeviceStatus { get { return _deviceStatus; } }

        public string FriendlyName
        {
            get { return _remoteEp.Address.ToString(); }
        }

        public ulong InputDataLength { get; }
        public ulong OutputDataLength { get; }

        private IPEndPoint _localEp, _remoteEp;
        private UdpClient _localUDPClient, _localUDPServer;
        private TcpListener _localTCPServer;
        private Socket _localTcpClient;

        public IPAddress RemoteIpAddress;

        private int _udpClientPort, _udpServerPort, _tcpServerPort;
        private byte[] _rxBufBytes;
        private readonly AutoResetEvent _isPendingCfgRecv = new AutoResetEvent(false);
        private readonly AutoResetEvent _isPendingBeaconRecv = new AutoResetEvent(false);

        private Status _deviceStatus;

        private Thread tcpMainThread;
        private Hashtable tcpSocketHolder = new Hashtable();
        private Hashtable tcpThreadHolder = new Hashtable();

        private NetworkStream _incomingStream;
        private byte[] _tcpRecvBytes = new byte[1024];

        private FileStream DebugDataStream;

        public CC3200(int UDPClientPort, int UDPServerPort, int TCPServerPort)
        {
            _udpClientPort = UDPClientPort;
            _udpServerPort = UDPServerPort;
            _tcpServerPort = TCPServerPort;

            Init();
            Connect();
        }

        public void Connect()
        {
            //throw new NotImplementedException();
            // Start UDP server to listening any device connection
            _localUDPServer.BeginReceive(onUDPreceived, null);
            // Start TCP server to listening to connections
            _localTCPServer.Start();
            _localTCPServer.BeginAcceptSocket(onTCPconnection, null);
        }

        private void onTCPconnection(IAsyncResult ar)
        {
            //throw new NotImplementedException();
            _localTcpClient = _localTCPServer.EndAcceptSocket(ar);
            MessageBox.Show("TCP connection: " + _localTcpClient.LocalEndPoint + "<---" + _localTcpClient.RemoteEndPoint);
            DebugDataStream = new FileStream("D:\\TCPDebug.txt", FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true);
            //_incomingStream = _localTcpClient.GetStream();
            _localTcpClient.BeginReceive(_tcpRecvBytes, 0, _tcpRecvBytes.Length, SocketFlags.None, OnTCPReceived, null);
        }

        private void OnTCPReceived(IAsyncResult ar)
        {
            //var len = _incomingStream.EndRead(ar);
            int bytesRead = _localTcpClient.EndReceive(ar);

            if (bytesRead > 0)
            {
                DebugDataStream.Write(_tcpRecvBytes.ToArray(), 0, bytesRead);
                //_localTCPServer.BeginAcceptSocket(onTCPconnection, null);
                _localTcpClient.BeginReceive(_tcpRecvBytes, 0, _tcpRecvBytes.Length, SocketFlags.None, OnTCPReceived, null);
                //_localTcpClient.Shutdown(SocketShutdown.Send);
            }
            else
            {
                _localTcpClient.Shutdown(SocketShutdown.Send);
                _localTCPServer.BeginAcceptSocket(onTCPconnection, null);
            }
            //Debug.WriteLine(Encoding.ASCII.GetString(_tcpRecvBytes, 0, _tcpRecvBytes.Length));
            //_incomingStream.BeginRead(_tcpRecvBytes, 0, _tcpRecvBytes.Length, OnTCPReceived, null);
        }


        private void onUDPreceived(IAsyncResult ar)
        {
            _rxBufBytes = _localUDPServer.EndReceive(ar, ref _remoteEp);

            //string message = Encoding.ASCII.GetString(_rxBufBytes, 0, _rxBufBytes.Length);

            if (_rxBufBytes.Length == 1)
            {
                switch (_rxBufBytes[0])
                {
                    case (byte)CC3200Msg.Connect:
                        if (!Equals(RemoteIpAddress, IPAddress.Any))
                            OnDeviceAttached();
                        if (!Equals(RemoteIpAddress, _remoteEp.Address))
                            OnDeviceIPchanged();
                        RemoteIpAddress = _remoteEp.Address;
                        MessageBox.Show("WiFi module connected on IP" + RemoteIpAddress);
                        _deviceStatus = Status.Configure;
                        break;
                    case (byte)CC3200Msg.Disconnect:
                        RemoteIpAddress = IPAddress.Any;
                        _deviceStatus = Status.Disconnected;
                        break;
                    case (byte)CC3200Msg.Beacon:
                        if (!Equals(RemoteIpAddress, _remoteEp.Address))
                        {
                            RemoteIpAddress = _remoteEp.Address;
                            OnDeviceIPchanged();
                        }
                        _isPendingBeaconRecv.Set();
                        break;
                    default:
                        _isPendingCfgRecv.Set();
                        break;
                }                   
            }
            else
            {
                _isPendingCfgRecv.Set();
            }

            _localUDPServer.BeginReceive(onUDPreceived, null);
        }

        public void Init()
        {
            //throw new NotImplementedException();
            RemoteIpAddress = IPAddress.Any;
            // Define IP End points
            _remoteEp = new IPEndPoint(IPAddress.Any, 0);
            _localEp = new IPEndPoint(IPAddress.Any, 0);
            // Setup UDP Server
            _localEp.Port = _udpServerPort;
            _localUDPServer = new UdpClient(_localEp);
            // Setup UDP Client
            _localEp.Port = _udpClientPort;
            _localUDPClient = new UdpClient();
            // Setup TCP server
            _localEp.Port = _tcpServerPort;
            _localTCPServer = new TcpListener(_localEp);
        }

        public void Deinit()
        {
            //throw new NotImplementedException();
            _localUDPServer.Close();
            _localUDPClient.Close();
            _localTCPServer.Stop();
            _deviceStatus = Status.Disconnected;
        }

        public void SwitchMode(Enum target, Enum targeMode, int deviceAddr)
        {
            //throw new NotImplementedException();
            var targetDevice = (Target)target;
            var mode = (Status)targeMode;
            if (targetDevice != Target.CC3200) return;
            _deviceStatus = mode;
            switch (_deviceStatus)
            {
                case Status.Configure:
                    cfg_send(Target.CC3200, BitConverter.GetBytes((ushort)CC3200Cmd.Set_Mode_Config), 1, deviceAddr);
                    break;
                case Status.StreamIn:
                    cfg_send(Target.CC3200, BitConverter.GetBytes((ushort)CC3200Cmd.Set_Mode_Readout), 1, deviceAddr);
                    break;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool IsASICConnected(int addr)
        {
            throw new NotImplementedException();
        }

        public event EventHandler DeviceAttached;
        public event EventHandler DeviceRemoved;
        public event EventHandler DeviceIPchanged;

        public void cfg_send(Enum targetDevice, byte[] content, int length, int channelAddr)
        {
            //throw new NotImplementedException();
            if ((_localUDPClient != null) && !Equals(_remoteEp.Address, IPAddress.Any))
            {
                try
                {
                    _remoteEp.Port = _udpClientPort;
                    _localUDPClient.BeginSend(content, length, _remoteEp, onUDPsend, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    _localUDPClient.Close();
                }
            }
        }

        private void onUDPsend(IAsyncResult ar)
        {
            //throw new NotImplementedException();
            int length = _localUDPClient.EndSend(ar);
            MessageBox.Show(length + " UDP Packets send.");
        }

        public void cfg_receive(Enum targetDevice, ref byte[] content, int length, int channelAddr)
        {
            //throw new NotImplementedException();
            // Send command for pooling
            cfg_send(CC3200.Target.CC3200, BitConverter.GetBytes((ushort)CC3200Cmd.Send_Readback), 1, channelAddr);
            _isPendingCfgRecv.WaitOne();    // Wait until UDP packets has been received.
            content = _rxBufBytes;
        }

        public void InitInputDataFlow(ITargetBlock<byte[]> targetDataflow, int queueSz, int ppx)
        {
            //throw new NotImplementedException();
        }

        public void StartInputDataFlow()
        {
            throw new NotImplementedException();
        }

        public void StopInputDataFlow()
        {
            throw new NotImplementedException();
        }

        public void InitOutputDataFlow(ITargetBlock<byte[]> targetDataflow)
        {
            throw new NotImplementedException();
        }

        public void StartOutputDataFlow()
        {
            throw new NotImplementedException();
        }

        public void StopOutputDataFlow()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            OnDeviceRemoved();
            Deinit();

            _remoteEp = null;
            _localEp = null;
            _localUDPClient = null;
            _localUDPServer = null;
            _localTCPServer = null;
        }

        protected virtual void OnDeviceAttached()
        {
            DeviceAttached?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDeviceRemoved()
        {
            DeviceRemoved?.Invoke(this, EventArgs.Empty);
        }

        public bool IsAgentConnected()
        {
            //throw new NotImplementedException(); 
            cfg_send(CC3200.Target.CC3200, BitConverter.GetBytes((ushort) CC3200Cmd.Send_Beacon), 1, 0);
            return _isPendingBeaconRecv.WaitOne(1000); // Wait until UDP packets has been received.
        }

        protected virtual void OnDeviceIPchanged()
        {
            DeviceIPchanged?.Invoke(this, EventArgs.Empty);
        }
    }
}