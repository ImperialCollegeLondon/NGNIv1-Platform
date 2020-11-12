using System;
using System.Runtime.InteropServices;
using System.Threading;
using ASIC_Interface;
using INTAN_RHD2000;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;
using SharpDX.WPF.Cameras;
using USBridge_WPF.Properties;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace USBridge_WPF
{

    public class WaveformScope : D3D11
    {
        private VertexShader _pVertexShader;
        private PixelShader _pPixelShader;
        private ConstantBuffer<Projections> _pConstantBuffer;
        private Matrix _projection;
        private Buffer _vertices, _vertices_spike;
        private DataStream _vertexStream, _vertexStream_spike;
        private readonly double _samplingRate;

        private uint _frameBufferLength;

        //private const int CHANNEL_COUNT = 1;
        private const int TIMEBASE = 0;
        //private int _step;
        private bool _isReset;
        //private readonly int[] _bufferEndPosition = new int[32];
        private ulong _frameBeginTime;
        private int _bufferEndPosition;
        private int[] _sweepStartPos = new int[32];
        private int[] _sweepEndPos = new int[32];
        //private int[] _overflowPosition = new int[32];
        //private int[] _bufferStartPosition = new int[32];

        private NeuralChannel.NeuralSignal _signalPoint = new NeuralChannel.NeuralSignal(0, 0);
        private NeuralChannel.SpikeData _spikeEvent = null;

        //private bool isFrameMoved = false;
        //private uint _displayEndTime = 0;
        //private double dispFrameMargin = 1.01;
        private double _targetWidth, _targetHeight;
        private readonly bool _isFixedDataToPixelMapping = true;

        private readonly Color4 _backgroundColor4 = Color4.Black;
        private readonly Color4 _wiperColor4 = Color4.Black;
        private readonly Color4 _initColor4 = Color4.Black;


        ///* Debug variables */
        //private List<NeuralChannel.neuralSignal>[] DebugList = new List<NeuralChannel.neuralSignal>[32];
        //private uint[] _previousTime = new uint[32];

        /* Downsample variables */
        //private static double downSamplingRate = 1;
        //public static double slotCount;
        //private int minDownsamplePoint = 1;
        //private static NeuralChannel.neuralSignal[][] procData = new NeuralChannel.neuralSignal[ChannelCount][];

        public static readonly Vector3[] ColourValues = new Vector3[]
        {
            new Vector3(0x9B/(float) 0xFF, 0xC4/(float) 0xFF, 0xE5/(float) 0xFF),
            new Vector3(0xF2/(float) 0xFF, 0x01/(float) 0xFF, 0x06/(float) 0xFF),
            new Vector3(0x04/(float) 0xFF, 0x64/(float) 0xFF, 0x0D/(float) 0xFF),
            new Vector3(0xFE/(float) 0xFF, 0xFB/(float) 0xFF, 0x0A/(float) 0xFF),
            new Vector3(0xFB/(float) 0xFF, 0x55/(float) 0xFF, 0x14/(float) 0xFF),
            new Vector3(0xE1/(float) 0xFF, 0x15/(float) 0xFF, 0xC0/(float) 0xFF),
            new Vector3(0x00/(float) 0xFF, 0x58/(float) 0xFF, 0x7F/(float) 0xFF),
            new Vector3(0x0B/(float) 0xFF, 0xC5/(float) 0xFF, 0x82/(float) 0xFF),
            new Vector3(0xFE/(float) 0xFF, 0xB8/(float) 0xFF, 0xC8/(float) 0xFF),
            new Vector3(0x9E/(float) 0xFF, 0x83/(float) 0xFF, 0x17/(float) 0xFF),
            new Vector3(0x01/(float) 0xFF, 0xB2/(float) 0xFF, 0xED/(float) 0xFF),
            new Vector3(0x84/(float) 0xFF, 0xEB/(float) 0xFF, 0x81/(float) 0xFF),
            new Vector3(0x58/(float) 0xFF, 0x01/(float) 0xFF, 0x8B/(float) 0xFF),
            new Vector3(0xB7/(float) 0xFF, 0x06/(float) 0xFF, 0x39/(float) 0xFF),
            new Vector3(0x70/(float) 0xFF, 0x3B/(float) 0xFF, 0x01/(float) 0xFF),
            new Vector3(0xF7/(float) 0xFF, 0xF1/(float) 0xFF, 0xDF/(float) 0xFF),
            new Vector3(0x11/(float) 0xFF, 0x8B/(float) 0xFF, 0x8A/(float) 0xFF),
            new Vector3(0x4A/(float) 0xFF, 0xFE/(float) 0xFF, 0xFA/(float) 0xFF),
            new Vector3(0xFC/(float) 0xFF, 0xB1/(float) 0xFF, 0x64/(float) 0xFF),
            new Vector3(0x79/(float) 0xFF, 0x6E/(float) 0xFF, 0xE6/(float) 0xFF),
            new Vector3(0xEC/(float) 0xFF, 0x88/(float) 0xFF, 0x88/(float) 0xFF),
            new Vector3(0x53/(float) 0xFF, 0xAA/(float) 0xFF, 0xED/(float) 0xFF),
            new Vector3(0xF9/(float) 0xFF, 0x54/(float) 0xFF, 0x75/(float) 0xFF),
            new Vector3(0x61/(float) 0xFF, 0xFC/(float) 0xFF, 0x03/(float) 0xFF),
            new Vector3(0x5D/(float) 0xFF, 0x96/(float) 0xFF, 0x08/(float) 0xFF),
            new Vector3(0xDE/(float) 0xFF, 0x98/(float) 0xFF, 0xFD/(float) 0xFF),
            new Vector3(0x98/(float) 0xFF, 0xA0/(float) 0xFF, 0x88/(float) 0xFF),
            new Vector3(0xA2/(float) 0xFF, 0x2A/(float) 0xFF, 0xED/(float) 0xFF),
            new Vector3(0x24/(float) 0xFF, 0x8A/(float) 0xFF, 0xD0/(float) 0xFF),
            new Vector3(0x5C/(float) 0xFF, 0x53/(float) 0xFF, 0x00/(float) 0xFF),
            new Vector3(0x9F/(float) 0xFF, 0x65/(float) 0xFF, 0x51/(float) 0xFF),
            new Vector3(0xBC/(float) 0xFF, 0xFE/(float) 0xFF, 0xC6/(float) 0xFF),
        };

        private int _voltageRange;
        private ulong _lastEventTime;
        private ulong _frameStartTime;
        private ulong _frameEndTime;

        private uint _address;
        private uint _channel;
        /// <summary>
        /// Real time view for raw neural signal and spike events
        /// </summary>
        public WaveformScope(
            double samplingRate,
            uint timeWindow, // unit ms;
            int voltageRange,
            double plotterWidth,
            double plotterHeight,
            uint address,
            uint channel)
        {
            //_isTemplateMatching = isTemplateMatching;
            _channel = channel;
            _address = address;
            _samplingRate = samplingRate;
            _voltageRange = voltageRange;
            if (_isFixedDataToPixelMapping)
            {
                //downSamplingRate = 1;
                //scale = 1000 / (samplingRate * timeWindow);

                _targetHeight = plotterHeight;
                _targetWidth = plotterWidth;

                //step = (int)(samplingRate * scale / 60);

                //bufferDataLength = 10 * plotterWidth;

                //downSamplingRate = Math.Ceiling((samplingRate * timeWindow / 1000) / targetWidth);
                //downSamplingRate = downSamplingRate < 1 ? downSamplingRate = 1 : downSamplingRate;

                //_step = (int)(samplingRate / 60);

                _frameBufferLength = (uint)(samplingRate * timeWindow / 1000);  // in number of sample
            }
            else
            {
                //downSamplingRate = 50;
                _frameBufferLength = (uint)(timeWindow * samplingRate);
            }


            // Initialise GPU

            using (var dg = new DisposeGroup())
            {
                // --- init shaders
                ShaderFlags sFlags = ShaderFlags.EnableStrictness;
                //ShaderFlags sFlags = ShaderFlags.None;

#if DEBUG_GPU
                sFlags |= ShaderFlags.Debug;
                Configuration.EnableObjectTracking = true;
#endif
                //var pVSBlob = dg.Add(ShaderBytecode.CompileFromFile("D:\\MyShader.fx", "VS", "vs_4_0", sFlags, EffectFlags.None));
                var pVsBlob = dg.Add(ShaderBytecode.Compile(Resources.MyShader, "VS", "vs_4_0", sFlags));
                var inputSignature = dg.Add(ShaderSignature.GetInputSignature(pVsBlob));
                _pVertexShader = new VertexShader(Device, pVsBlob);

                //var pPSBlob = dg.Add(ShaderBytecode.CompileFromFile("D:\\MyShader.fx", "PS", "ps_4_0", sFlags, EffectFlags.None));
                var pPsBlob = dg.Add(ShaderBytecode.Compile(Resources.MyShader, "PS", "ps_4_0", sFlags));
                _pPixelShader = new PixelShader(Device, pPsBlob);

                // --- let DX know about the pixels memory layout
                var layout = new InputLayout(Device, inputSignature, new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                });
                Device.ImmediateContext.InputAssembler.InputLayout = (layout);
                dg.Add(layout);


                DepthStencilStateDescription desc = DepthStencilStateDescription.Default();
                desc.IsDepthEnabled = false;

                DepthStencilState state = new DepthStencilState(Device, desc);
                Device.ImmediateContext.OutputMerger.SetDepthStencilState(state);
                dg.Add(state);

                //
                // --- init vertices
                // 

                //int bufferSize = isTemplateMatching
                //    ? (int) (VectorColor.SIZE_IN_BYTES * _frameBufferLength) * 2
                //    : (int) (VectorColor.SIZE_IN_BYTES * _frameBufferLength); // For 32 channels

                int bufferSize = (int) (VectorColor.SIZE_IN_BYTES * _frameBufferLength);
                int bufferSize_spike = (int) (VectorColor.SIZE_IN_BYTES * _frameBufferLength) * 3;

                //int BufferSize = VectorColor.SizeInBytes * 10 * (int)samplingRate;  // For 32 channels
                _vertexStream = new DataStream(bufferSize, true, true);
                _vertexStream_spike = new DataStream(bufferSize_spike, true, true);

                _vertices = new Buffer(Device, _vertexStream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = bufferSize,
                    Usage = ResourceUsage.Dynamic,
                }
                    );
                _vertices_spike = new Buffer(Device, _vertexStream_spike, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = bufferSize_spike,
                    Usage = ResourceUsage.Dynamic,
                }
    );

                // Change before drawing
                //Device.ImmediateContext.InputAssembler.SetVertexBuffers(0,
                //    new VertexBufferBinding(_vertices, VectorColor.SIZE_IN_BYTES, 0));
                //Device.ImmediateContext.InputAssembler.PrimitiveTopology = isTemplateMatching
                //    ? (PrimitiveTopology.LineList)
                //    : (PrimitiveTopology.LineStrip);

                // --- create the constant buffer
                _pConstantBuffer = new ConstantBuffer<Projections>(Device);
                Device.ImmediateContext.VertexShader.SetConstantBuffer(0, _pConstantBuffer.Buffer);
            }

            Camera = new FirstPersonCamera();
            //Camera.SetProjParams((float)Math.PI / 2, 1, 0.01f, 100.0f);
            Camera.SetViewParams(new Vector3(0.0f, 0.0f, -5.0f), new Vector3(0.0f, 0.0f, 0.0f), Vector3.UnitY);

            // input initial values to buffer;
            /*
             * Signals
             */
            Device.ImmediateContext.InputAssembler.SetVertexBuffers(0,
                new VertexBufferBinding(_vertices, VectorColor.SIZE_IN_BYTES, 0));
            Device.ImmediateContext.InputAssembler.PrimitiveTopology = (PrimitiveTopology.LineStrip);
            Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, MapFlags.None, out _vertexStream);
            _vertexStream.Position = 0;
            for (var i = 0; i < _frameBufferLength; i++)
            {
                _vertexStream.Write(
                    new VectorColor(new Vector3(i, //X
                        0.4f * ((float)_targetHeight), //Y
                        0.5f), // Z
                        _initColor4));
            }

            Device.ImmediateContext.UnmapSubresource(_vertices, 0);

            /*
             * Spikes
             */
            Device.ImmediateContext.InputAssembler.SetVertexBuffers(0,
            new VertexBufferBinding(_vertices_spike, VectorColor.SIZE_IN_BYTES, 0));
            Device.ImmediateContext.InputAssembler.PrimitiveTopology = (PrimitiveTopology.TriangleList);
            Device.ImmediateContext.MapSubresource(_vertices_spike, MapMode.WriteDiscard, MapFlags.None, out _vertexStream_spike);
            _vertexStream_spike.Position = 0;
            for (var i = 0; i < _frameBufferLength; i++)
            {
                _vertexStream_spike.WriteRange(new[]
                {
                    new VectorColor(new Vector3(i, //X
                        ((float) _targetHeight), //Y
                        0.5f), // Z
                        _initColor4),
                    new VectorColor(new Vector3(i, //X
                        (float) _targetHeight, //Y
                        0.5f), // Z
                        _initColor4),
                    new VectorColor(new Vector3(i, //X
                        (float) _targetHeight, //Y
                        0.5f), // Z
                        _initColor4)
                }
                    );
            }

            Device.ImmediateContext.UnmapSubresource(_vertices_spike, 0);
        }

        private bool _isSync = false;

        public bool IsSync
        {
            get { return _isSync; }
            set { _isSync = value; }
        }

        /// <summary>
        /// Reset the canvas, mapping pixels to vectors
        /// </summary>
        public override void Reset(int w, int h)
        {
            base.Reset(w, h);

            _projection = _isFixedDataToPixelMapping ? Matrix.OrthoLH(_frameBufferLength, (float)_targetHeight, 0f, 100f) : Matrix.OrthoLH(w, h, 0f, 100f);

            _isReset = true;
            _isSync = false;    // fix out of sync problem during resize.
        }


        /// <summary>
        /// Main render function
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void RenderScene(DrawEventArgs args)
        {
            // Get the render target size only once per frame.

            if (!_isFixedDataToPixelMapping)
            {
                _targetWidth = RenderTarget.Description.Width;
                _targetHeight = RenderTarget.Description.Height;
            }

            if (_isReset)
            {
                //var g_World = Matrix.Multiply(Matrix.Scaling(1), Matrix.Translation(-targetWidth / 2 - timebase, -targetHeight / 2, 0));
                Matrix gWorld = Matrix.Translation(-_frameBufferLength / 2 - TIMEBASE, -(float)_targetHeight / 2, 0);

                //
                // Update variables
                //
                _pConstantBuffer.Value = new Projections
                {
                    World = Matrix.Transpose(gWorld),
                    View = Matrix.Transpose(Camera.View),
                    Projection = Matrix.Transpose(_projection),
                };
            }

            //
            // Update vertices
            //

            // Augment data, no need to discard the whole buffer
            Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteNoOverwrite, MapFlags.None, out _vertexStream);
            Device.ImmediateContext.MapSubresource(_vertices_spike, MapMode.WriteNoOverwrite, MapFlags.None, out _vertexStream_spike);

            #region update value

            //_displayEndTime = uint.MaxValue; // Set the end time to maximum so it can get update to the correct value.

            // Load the data from CPU
            var displayAddr = RealtimeResults.DisplayAddr;

            if (MainWindow.NeuralChannel[displayAddr, _channel] != null)    // skip not active channels
            {
                // Load the old position
                _vertexStream.Position = _bufferEndPosition * VectorColor.SIZE_IN_BYTES;
                _vertexStream_spike.Position = _bufferEndPosition * VectorColor.SIZE_IN_BYTES * 3;

                // Get new data length
                //if (DataLength == -1)

                #region write points to GPU buffer

                var dataLength = (uint) MainWindow.NeuralChannel[displayAddr, _channel].NeuralSignals.Count;
                if (dataLength != 0 && !MainWindow.IsSpikeSorting)
                {
                    #region Neural signal with/without spike annotation

                    // Data is more than one frame. clear and only draw the most recent one
                    var limit = (dataLength / _frameBufferLength);
                    if (limit > 0)
                    {
                        // Dump all the data that won't be displayed
                        for (var i = 0; i < limit * _frameBufferLength; i++)
                        {
                            MainWindow.NeuralChannel[displayAddr, _channel].NeuralSignals.TryDequeue(out _signalPoint);
                            dataLength--;
                        }
                        //// Adjust the display start time (in samples)
                        //WaveformScopeView.FrameBeginTime += limit * _frameBufferLength;
                        // Mark the display position as out of sync
                        _isSync = false;
                    }

                    /* 
                     * Start update GPU data stream with the most recent data
                     */

                    // Get one spike event (could be null)
                    //MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                    //_lastEventTime = MainWindow.NeuralChannel[displayAddr, _channel].LastEventTime;

                    // Get new spike if there isn't one waiting to be plotted and there is one in queue
                    if (_spikeEvent == null && MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.Count != 0)
                        MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);

                    // BLOCK A was originally here
                    
                    if (!_isSync)
                    {
                        //Todo Correct frame overflow caculation, otherwise, it will break;
                        _bufferEndPosition = (int)(_signalPoint.Time % _frameBufferLength);
                        _vertexStream.Position = _bufferEndPosition * VectorColor.SIZE_IN_BYTES;
                        _vertexStream_spike.Position = 3 * _bufferEndPosition * VectorColor.SIZE_IN_BYTES;
                        _frameBeginTime = (_signalPoint.Time / _frameBufferLength) *
                                          _frameBufferLength; // Find the frame begin time in unit of samples.

                        _isSync = true;
                    }

                    // LABEL --- BLOCK A
                    // Dump all the spike events that won't be displayed
                    //while ((_spikeEvent != null) && ((_spikeEvent.Time < WaveformScopeView.FrameBeginTime) || (_spikeEvent.TemplateId == 4)))
                    while ((_spikeEvent != null) && (_spikeEvent.Time < _frameBeginTime))
                    {
                        // AB TODO WARNING: playing with the following line
                        MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        if (_spikeEvent == null) _isSync = false;
                    }

                    // Start copy neural signals
                    for (var i = 0; i < dataLength; i++)
                    {
                        MainWindow.NeuralChannel[displayAddr, _channel].NeuralSignals.TryDequeue(out _signalPoint);
                        _vertexStream.Write(
                            new VectorColor(new Vector3(_bufferEndPosition, //X
                                    (float)Math.Min(
                                        (((_signalPoint.Amplitude / MainWindow.INTAN[_address].FullRangeBit - 0.5) * 
                                        (MainWindow.INTAN[_address].FullRangeValue / _voltageRange) * 0.8 * _targetHeight) + 0.4 * _targetHeight), 
                                        0.8*_targetHeight), //Y
                                    0.5f), // Z
                                new Color4(ColourValues[_channel], 1.0f))); // Colour

                        // Check if there is a spike at this position
                        if (_spikeEvent != null && _bufferEndPosition == (int) (_spikeEvent.Time % _frameBufferLength) && (_spikeEvent.Time <= _frameBeginTime + _frameBufferLength) && (_spikeEvent.Time >= _frameBeginTime))
                        {
                            // write spike event annotation
                            _vertexStream_spike.WriteRange(new[]
                            {
                                // Triangle Point 1
                                new VectorColor(new Vector3(_bufferEndPosition + 300, //X
                                        (_spikeEvent.TemplateId + 1) * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                // Colour

                                // Triangle Point 2
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        _spikeEvent.TemplateId * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                // Colour

                                // Triangle Point 3
                                new VectorColor(new Vector3(_bufferEndPosition - 300, //X
                                        (_spikeEvent.TemplateId + 1) * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f))
                                // Colour
                            });

                            MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        }
                        else
                        {
                            // write invisible event annotation (shrinked triangle)
                            _vertexStream_spike.WriteRange(new[]
                            {
                                // Triangle Point 1
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4),
                                // Colour

                                // Triangle Point 2
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4),
                                // Colour

                                // Triangle Point 3
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4)
                                // Colour
                            });

                            // Get next point
                            if (_spikeEvent != null &&
                                _spikeEvent.Time % _frameBufferLength < (ulong) _bufferEndPosition)
                                MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        }

                        _bufferEndPosition++;

                        if (_bufferEndPosition == _frameBufferLength)
                        {
                            _bufferEndPosition = 0;
                            _vertexStream.Position = 0;
                            _vertexStream_spike.Position = 0;
                            _frameBeginTime += _frameBufferLength;
                        }
                    }

                    #region write wiping region

                    for (int i = 0; i < 3 * _frameBufferLength / _targetWidth; i++)
                        // Width is 3 pixels wide (_frameBufferLength / _targetWidth = number of data points / pixel)
                        //for (int i = 0; i < 10 ; i++)
                    {
                        if (_bufferEndPosition + i == _frameBufferLength)
                        {
                            // Reset stream position to the start of the channel
                            _vertexStream.Position = 0;
                            _vertexStream_spike.Position = 0;
                        }

                        _vertexStream.Write(
                            new VectorColor(
                                new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    0.4f * ((float) _targetHeight), //Y
                                    0.5f), // Z
                                _wiperColor4));

                        _vertexStream_spike.WriteRange(new[]
                        {
                            // Vertical line strip start point
                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4),

                            // Vertical line Strip end point
                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4),

                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4)
                        });
                    }

                    #endregion
                #endregion
                }
                else
                {
                    #region Spike events only
                    // Get new spike if there isn't one waiting to be plotted and there is one in queue
                    if (_spikeEvent == null && MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.Count != 0)
                        MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);

                    // Dump all the spike events that won't be displayed
                    //while ((_spikeEvent != null) && ((_spikeEvent.Time < WaveformScopeView.FrameBeginTime) || (_spikeEvent.TemplateId == 4)))
                    while ((_spikeEvent != null) && (_spikeEvent.Time < _frameBeginTime))
                    {
                        MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        if (_isSync) _isSync = false;
                    }

                    if ((_spikeEvent != null) && !_isSync)
                    {
                        //Todo Correct frame overflow caculation, otherwise, it will break;
                        _bufferEndPosition = (int)(_spikeEvent.Time % _frameBufferLength);
                        _vertexStream.Position = _bufferEndPosition * VectorColor.SIZE_IN_BYTES;
                        _vertexStream_spike.Position = 3 * _bufferEndPosition * VectorColor.SIZE_IN_BYTES;
                        _frameBeginTime = (_spikeEvent.Time / _frameBufferLength) *
                                          _frameBufferLength; // Find the frame begin time in unit of samples.

                        _isSync = true;
                    }

                    // Display all the visible ones.
                    while (_spikeEvent != null &&
                            (_spikeEvent.Time <= _frameBeginTime + _frameBufferLength) &&
                            (_spikeEvent.Time >= _frameBeginTime))
                    {
                        // Write invisible signal traces
                        _vertexStream.Write(
                            new VectorColor(new Vector3(_bufferEndPosition, //X
                                    (float)Math.Min(
                                    (((_signalPoint.Amplitude / MainWindow.INTAN[_address].FullRangeBit - 0.5) *
                                    (MainWindow.INTAN[_address].FullRangeValue / _voltageRange) * 0.8 * _targetHeight) + 0.4 * _targetHeight), 
                                    0.8 * _targetHeight),//Y
                                    0.5f), // Z
                                    _wiperColor4)); // Colour

                        // Check if there is a spike at this position
                        if (_bufferEndPosition == (int) (_spikeEvent.Time % _frameBufferLength))
                        {
                            // write spike event annotation
                            _vertexStream_spike.WriteRange(new[]
                            {
                                // Triangle Point 1
                                new VectorColor(new Vector3(_bufferEndPosition + 300, //X
                                        (_spikeEvent.TemplateId + 1) * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                // Colour

                                // Triangle Point 2
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        _spikeEvent.TemplateId * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                // Colour

                                // Triangle Point 3
                                new VectorColor(new Vector3(_bufferEndPosition - 300, //X
                                        (_spikeEvent.TemplateId + 1) * (0.2f * (float) _targetHeight / 4) +
                                        0.8f * (float) _targetHeight, //Y
                                        0.5f), // Z
                                    new Color4(ColourValues[(_channel * 4 + _spikeEvent.TemplateId) % 32], 1.0f))
                                // Colour
                            });

                            // Get next point
                            MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        }
                        else
                        {
                            // write invisible event annotation (shrinked triangle)
                            _vertexStream_spike.WriteRange(new[]
                            {
                                // Triangle Point 1
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4),
                                // Colour

                                // Triangle Point 2
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4),
                                // Colour

                                // Triangle Point 3
                                new VectorColor(new Vector3(_bufferEndPosition, //X
                                        (float) _targetHeight, //Y
                                        0.5f), // Z
                                    _backgroundColor4)
                                // Colour
                            });

                            // Get next point
                            if (_spikeEvent != null &&
                                _spikeEvent.Time % _frameBufferLength < (ulong) _bufferEndPosition)
                                MainWindow.NeuralChannel[displayAddr, _channel].SpikeEvents.TryDequeue(out _spikeEvent);
                        }

                        // Advance display buffer position
                        _bufferEndPosition++;

                        if (_bufferEndPosition == _frameBufferLength)
                        {
                            _bufferEndPosition = 0;
                            _vertexStream.Position = 0;
                            _vertexStream_spike.Position = 0;
                            _frameBeginTime += _frameBufferLength;
                        }
                    }

                    if (_spikeEvent != null)
                    {
                        // Spike is out of current display frame
                        _bufferEndPosition = 0;
                        _vertexStream.Position = 0;
                        _vertexStream_spike.Position = 0;
                        while (_spikeEvent.Time > _frameBeginTime + _frameBufferLength)
                            _frameBeginTime += _frameBufferLength;
                    }

                    #endregion

                    #region write wiping region

                    for (int i = 0; i < 3 * _frameBufferLength / _targetWidth; i++)
                    // Width is 3 pixels wide (_frameBufferLength / _targetWidth = number of data points / pixel)
                    //for (int i = 0; i < 10 ; i++)
                    {
                        if (_bufferEndPosition + i == _frameBufferLength)
                        {
                            // Reset stream position to the start of the channel
                            _vertexStream.Position = 0;
                            _vertexStream_spike.Position = 0;
                        }

                        _vertexStream.Write(
                            new VectorColor(
                                new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    0.4f * ((float)_targetHeight), //Y
                                    0.5f), // Z
                                _wiperColor4));

                        _vertexStream_spike.WriteRange(new[]
                        {
                            // Vertical line strip start point
                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4),

                            // Vertical line Strip end point
                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4),

                            new VectorColor(new Vector3((_bufferEndPosition + i) % _frameBufferLength, //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                _wiperColor4)
                        });
                    }

                    #endregion
                }
            }

            #endregion

            #endregion
            Device.ImmediateContext.UnmapSubresource(_vertices_spike, 0);
            Device.ImmediateContext.UnmapSubresource(_vertices, 0);

            //
            // Configure GPU pipeline
            //
            Device.ImmediateContext.VertexShader.Set(_pVertexShader);
            if (_isReset)
            {
                Device.ImmediateContext.VertexShader.SetConstantBuffer(0, _pConstantBuffer.Buffer);
            }
            Device.ImmediateContext.PixelShader.Set(_pPixelShader);

            // Draw
            #region draw
            Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, _backgroundColor4);

            Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices, VectorColor.SIZE_IN_BYTES, 0));
            Device.ImmediateContext.InputAssembler.PrimitiveTopology = (PrimitiveTopology.LineStrip);
            Device.ImmediateContext.Draw((int)_frameBufferLength, 0);

            Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices_spike, VectorColor.SIZE_IN_BYTES, 0));
            Device.ImmediateContext.InputAssembler.PrimitiveTopology = (PrimitiveTopology.TriangleList);
            Device.ImmediateContext.Draw((int)_frameBufferLength*3, 0);

            _isReset = false;

            #endregion
        }

        //public void UpdateTimeSpan(int timeWindow)
        //{
        //    _frameBufferLength = (uint)(_samplingRate * timeWindow / 1000);
        //    _bufferEndPosition = 0;
        //    // input initial values to buffer;
        //    Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, MapFlags.None, out _vertexStream);

        //        for (var i = 0; i < _frameBufferLength; i++)
        //        {
        //            _vertexStream.Write(
        //                new VectorColor(new Vector3(i, //X
        //                    0.5f * ((float) _targetHeight), //Y
        //                    0.5f), // Z
        //                    _backgroundColor4));
        //        }

        //    Device.ImmediateContext.UnmapSubresource(_vertices, 0);
        //}

        //public void UpdateVoltageRagne(int voltageRange)
        //{
        //    _voltageRange = voltageRange;
        //}

        /// <summary>
        /// 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            // NOTE: SharpDX 1.3 requires explicit Dispose() of everything
            Utilities.Dispose(ref _pConstantBuffer);
            Utilities.Dispose(ref _vertices);
            Utilities.Dispose(ref _vertices_spike);
            Utilities.Dispose(ref _vertexStream);
            Utilities.Dispose(ref _vertexStream_spike);
            Utilities.Dispose(ref _pPixelShader);
            Utilities.Dispose(ref _pVertexShader);
            base.Dispose(disposing);
            //Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
        }
    }
}