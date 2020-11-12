using System;
using System.Runtime.InteropServices;
using ASIC_Interface;
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

    public class LiveView : D3D11
    {
        private VertexShader _pVertexShader;
        private PixelShader _pPixelShader;
        private ConstantBuffer<Projections> _pConstantBuffer;
        private Matrix _projection;
        private Buffer _vertices;
        private DataStream _vertexStream;
        private readonly double _samplingRate;

        private uint _frameBufferLength;
        private uint _frameTimeSpan;

        private const int CHANNEL_COUNT = 32;
        private const int TIMEBASE = 0;
        //private int _step;
        private bool _isReset;
        private readonly int[] _bufferEndPosition = new int[32];
        private int[] _sweepStartPos = new int[32];
        private int[] _sweepEndPos = new int[32];
        //private int[] _overflowPosition = new int[32];
        //private int[] _bufferStartPosition = new int[32];

        private NeuralChannel.NeuralSignal _signalPoint = new NeuralChannel.NeuralSignal(0, 0);
        private NeuralChannel.SpikeData _spikeEvent = new NeuralChannel.SpikeData(0, 0);

        //private bool isFrameMoved = false;
        //private uint _displayEndTime = 0;
        //private double dispFrameMargin = 1.01;
        private double _targetWidth, _targetHeight;
        private readonly bool _isFixedDataToPixelMapping = true;

        ///* Debug variables */
        //private List<NeuralChannel.neuralSignal>[] DebugList = new List<NeuralChannel.neuralSignal>[32];
        //private uint[] _previousTime = new uint[32];

        /* Downsample variables */
        //private static double downSamplingRate = 1;
        //public static double slotCount;
        //private int minDownsamplePoint = 1;
        //private static NeuralChannel.neuralSignal[][] procData = new NeuralChannel.neuralSignal[ChannelCount][];

        private static readonly Vector3[] ColourValues = new Vector3[]
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
        private readonly bool _isTemplateMatching;
        private ulong _lastEventTime;
        private ulong _frameStartTime;
        private ulong _frameEndTime;

        /// <summary>
        /// Real time view for raw neural signal and spike events
        /// </summary>
         public LiveView(
             double samplingRate,
             uint timeWindow, // uint ms;
             int voltageRange,
             double plotterWidth,
             double plotterHeight,
             bool isTemplateMatching)
         {
             _isTemplateMatching = isTemplateMatching;
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

                _frameTimeSpan = timeWindow * 16;   // time resolution is 62.5 us, 16 * 62.5 us = 1ms.
                _frameBufferLength = (uint)(samplingRate * timeWindow / 1000);
               
            }
            else
            {
                //downSamplingRate = 50;
                _frameBufferLength = (uint) (timeWindow * samplingRate);
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
                var layout = new InputLayout(Device, inputSignature, new[]{
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

                int bufferSize = (int) (VectorColor.SIZE_IN_BYTES * _frameBufferLength * 32);  // For 32 channels
                //int BufferSize = VectorColor.SizeInBytes * 10 * (int)samplingRate * 32;  // For 32 channels
                _vertexStream = new DataStream(bufferSize, true, true);
                _vertices = new Buffer(Device, _vertexStream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = bufferSize,
                    Usage = ResourceUsage.Dynamic,
                }
                    );

                Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices, VectorColor.SIZE_IN_BYTES, 0));
                Device.ImmediateContext.InputAssembler.PrimitiveTopology = isTemplateMatching ? (PrimitiveTopology.LineList) : (PrimitiveTopology.LineStrip);

                // --- create the constant buffer
                _pConstantBuffer = new ConstantBuffer<Projections>(Device);
                Device.ImmediateContext.VertexShader.SetConstantBuffer(0, _pConstantBuffer.Buffer);
            }

            Camera = new FirstPersonCamera();
            //Camera.SetProjParams((float)Math.PI / 2, 1, 0.01f, 100.0f);
            Camera.SetViewParams(new Vector3(0.0f, 0.0f, -5.0f), new Vector3(0.0f, 0.0f, 0.0f), Vector3.UnitY);

            // input initial values to buffer;
            Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, MapFlags.None, out _vertexStream);

            for (var channelId = 0; channelId < CHANNEL_COUNT; channelId++)
            {
                for (var i = 0; i < _frameBufferLength; i++)
                {
                    _vertexStream.Write(
                        new VectorColor(new Vector3(i, //X
                        (channelId + 0.5f) * ((float)_targetHeight / CHANNEL_COUNT), //Y
                        0.5f),  // Z
                        new Color4(1.0f, 1.0f, 1.0f, 1.0f)));    // White colour
                }
            }

            Device.ImmediateContext.UnmapSubresource(_vertices, 0);
        }

        /// <summary>
        /// Reset the canvas, mapping pixels to vectors
        /// </summary>
        public override void Reset(int w, int h)
        {
            base.Reset(w, h);

            _projection = _isFixedDataToPixelMapping ? Matrix.OrthoLH(_frameBufferLength, (float)_targetHeight, 0f, 100f) : Matrix.OrthoLH(w, h, 0f, 100f);

            _isReset = true;
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

            #region update value

            //_displayEndTime = uint.MaxValue; // Set the end time to maximum so it can get update to the correct value.

            // Load the data from CPU
            var displayAddr = RealtimeResults.DisplayAddr;
            for (var channelId = 0; channelId < CHANNEL_COUNT; channelId++)
            {
                if (MainWindow.NeuralChannel[displayAddr, channelId] == null)
                {
                    // skip not active channels
                }
                else
                {
                    // Load the old position
                    _vertexStream.Position = (_bufferEndPosition[channelId] + channelId * _frameBufferLength) * VectorColor.SIZE_IN_BYTES;

                    // Get new data length
                    //if (DataLength == -1)

                    #region write points to GPU buffer

                    if (_isTemplateMatching)
                    {
                        #region Spike Events

                        // Adjust frame based on time base

                        //if (MainWindow.TimeBase > _frameEndTime)
                        //{
                        //    _frameStartTime = (MainWindow.TimeBase / _frameTimeSpan) * _frameTimeSpan;
                        //    _frameEndTime = _frameStartTime + _frameTimeSpan;
                        //}

                        // Check spike events in terms of buffer length
                        var dataLength = (uint)MainWindow.NeuralChannel[displayAddr, channelId].SpikeEvents.Count;
                        if (dataLength > 0)
                        {
                            _lastEventTime = MainWindow.NeuralChannel[displayAddr, channelId].LastEventTime;
                            //_frameStartTime = _frameRefreshCount * _frameTimeSpan;
                            //_frameEndTime = _frameStartTime + _frameTimeSpan;

                            // Further frame time adjustion according to last spike event if necessary
                            //while (_lastEventTime > _frameEndTime)
                            //{
                            //    _frameStartTime += _frameTimeSpan;
                            //    _frameEndTime = _frameStartTime + _frameTimeSpan;
                            //}

                            //MainWindow.NeuralChannel[0, channelId].SpikeSignal.TryDequeue(out _spikeEvent);

                            //while ((_spikeEvent != null) && (_spikeEvent.Time <= _lastEventTime))
                            //{
                            //    _vertexStream.WriteRange(new[]
                            //        {
                            //            // Vertical line strip start point
                            //            new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                            //                (channelId * 4 + _spikeEvent.TemplateId) *
                            //                ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                            //                0.5f), // Z
                            //                new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                            //            // Colour

                            //            // Vertical line Strip end point
                            //            new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                            //                (channelId * 4 + _spikeEvent.TemplateId + 1) *
                            //                ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                            //                0.5f), // Z
                            //                new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f))
                            //            // Colour
                            //        });

                            //    MainWindow.NeuralChannel[0, channelId].SpikeSignal.TryDequeue(out _spikeEvent);
                            //}

                            //_bufferEndPosition[channelId] += 2;

                            if (_lastEventTime >= _frameEndTime + _frameTimeSpan)
                            {
                                // The latest spike event has a timestamp newer even than the next frame

                                // First adjust frame start and end time such that all latest spike events will be displayed
                                do
                                {
                                    _frameStartTime += _frameTimeSpan;
                                    _frameEndTime = _frameStartTime + _frameTimeSpan;
                                } while (_lastEventTime > _frameEndTime);

                                // Clear spike events that won't be displayed form the buffer
                                do
                                {
                                    MainWindow.NeuralChannel[displayAddr, channelId].SpikeEvents.TryDequeue(
                                        out _spikeEvent);
                                } while (_spikeEvent.Time < _frameStartTime);


                                // Start at the beginning of the buffer
                                _bufferEndPosition[channelId] = 0;
                                _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;

                                while ((_spikeEvent != null) && (_spikeEvent.Time <= _lastEventTime))
                                {
                                    _vertexStream.WriteRange(new[]
                                    {
                                        // Vertical line strip start point
                                        new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                                            (channelId * 4 + _spikeEvent.TemplateId) *
                                            ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                                            0.5f), // Z
                                            new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                        // Colour

                                        // Vertical line Strip end point
                                        new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                                            (channelId * 4 + _spikeEvent.TemplateId + 1) *
                                            ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                                            0.5f), // Z
                                            new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f))
                                        // Colour
                                    });

                                    _bufferEndPosition[channelId] += 2;

                                    MainWindow.NeuralChannel[displayAddr, channelId].SpikeEvents.TryDequeue(
                                        out _spikeEvent);
                                }
                            }
                            else
                            {
                                // new spike events will be displayed before next frame ends
                                do
                                {
                                    MainWindow.NeuralChannel[displayAddr, channelId].SpikeEvents.TryDequeue(
                                        out _spikeEvent);

                                    // Adjust time markings
                                    if (_spikeEvent.Time > _frameEndTime)
                                    {
                                        _frameStartTime += _frameTimeSpan;
                                        _frameEndTime = _frameStartTime + _frameTimeSpan;
                                        _bufferEndPosition[channelId] = 0;
                                        _vertexStream.Position = channelId * _frameBufferLength *
                                                                 VectorColor.SIZE_IN_BYTES;
                                    }

                                    // Adjust buffer positions. Usually don't need, but as a precaution
                                    if (_bufferEndPosition[channelId] >= _frameBufferLength)
                                    {
                                        _bufferEndPosition[channelId] = 0;
                                        _vertexStream.Position = channelId * _frameBufferLength *
                                                                 VectorColor.SIZE_IN_BYTES;
                                    }

                                    _vertexStream.WriteRange(new[]
                                    {
                                        // Vertical line strip start point
                                        new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                                            (channelId * 4 + _spikeEvent.TemplateId) *
                                            ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                                            0.5f), // Z
                                            new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f)),
                                        // Colour

                                        // Vertical line Strip end point
                                        new VectorColor(new Vector3(_spikeEvent.Time - _frameStartTime, //X
                                            (channelId * 4 + _spikeEvent.TemplateId + 1) *
                                            ((float) _targetHeight / (4 * CHANNEL_COUNT)), //Y
                                            0.5f), // Z
                                            new Color4(ColourValues[(channelId * 4 + _spikeEvent.TemplateId) % 32], 1.0f))
                                        // Colour
                                    });

                                    _bufferEndPosition[channelId] += 2;
                                } while ((_spikeEvent != null) && (_spikeEvent.Time < _lastEventTime));
                            }
                        }
                        else
                        {
                            continue;
                        }


                        //if (dataLength / _frameBufferLength > 1)
                        //{
                        //    // Data is more than one frame. clear and only draw the most recent one
                        //    var limit = (dataLength / _frameBufferLength) * _frameBufferLength;
                        //    for (var i = 0; i < limit; i++)
                        //    {
                        //        MainWindow.NeuralChannel[0, channelId].RawSignal.TryDequeue(out _signalPoint);
                        //        dataLength--;
                        //    }

                        //    // Write mapped GPU buffer from beginning
                        //    _bufferEndPosition[channelId] = 0;
                        //    _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;
                        //}

                        // Check spike events in terms of display time span

                        #endregion
                    }
                    else
                    {
                        #region Neural Signals

                        var dataLength = (uint)MainWindow.NeuralChannel[displayAddr, channelId].NeuralSignals.Count;
                        if (dataLength / _frameBufferLength > 1)
                        {
                            // Data is more than one frame. clear and only draw the most recent one
                            var limit = (dataLength / _frameBufferLength) * _frameBufferLength;
                            for (var i = 0; i < limit; i++)
                            {
                                MainWindow.NeuralChannel[displayAddr, channelId].NeuralSignals.TryDequeue(out _signalPoint);
                                dataLength--;
                            }

                            // Write mapped GPU buffer from beginning
                            _bufferEndPosition[channelId] = 0;
                            _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;

                            for (var i = 0; i < dataLength; i++)
                            {
                                MainWindow.NeuralChannel[displayAddr, channelId].NeuralSignals.TryDequeue(out _signalPoint);
                                _vertexStream.Write(
                                    new VectorColor(new Vector3(_bufferEndPosition[channelId]++, //X
                                        (channelId + _signalPoint.Amplitude / (float) _voltageRange) *
                                        ((float) _targetHeight / CHANNEL_COUNT), //Y
                                        0.5f), // Z
                                        new Color4(ColourValues[channelId], 1.0f))); // Colour
                            }
                        }
                        else
                        {
                            // New data will not overflow
                            for (int i = 0; i < dataLength; i++)
                            {
                                if (_bufferEndPosition[channelId] == _frameBufferLength)
                                {
                                    _bufferEndPosition[channelId] = 0;
                                    _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;
                                }

                                MainWindow.NeuralChannel[displayAddr, channelId].NeuralSignals.TryDequeue(out _signalPoint);
                                _vertexStream.Write(
                                    new VectorColor(new Vector3(_bufferEndPosition[channelId]++, //X
                                        (channelId + _signalPoint.Amplitude / (float) _voltageRange) *
                                        ((float) _targetHeight / CHANNEL_COUNT), //Y
                                        0.5f), // Z
                                        new Color4(ColourValues[channelId], 1.0f))); // Colour
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region write sweeping region

                    for (int i = 0; i < 3 * _frameBufferLength / _targetWidth; i++)
                        // Width is 3 pixels wide (_frameBufferLength / _targetWidth = number of data points / pixel)
                        //for (int i = 0; i < 10 ; i++)
                    {
                        if (_isTemplateMatching)
                        {
                            if (_bufferEndPosition[channelId] + 2 * i >= _frameBufferLength)
                            {
                                // Reset stream position to the start of the channel
                                _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;
                            }

                            _vertexStream.WriteRange(new[]
                            {
                                // Vertical line strip start point
                                new VectorColor(new Vector3((float)(_lastEventTime + (ulong)(i % _frameTimeSpan)), //X
                                    0, //Y
                                    0.5f), // Z
                                    new Color4(1.0f, 1.0f, 1.0f, 1.0f)), // White colour

                                // Vertical line Strip end point
                                new VectorColor(new Vector3((float)(_lastEventTime + (ulong)i), //X
                                    (float) _targetHeight, //Y
                                    0.5f), // Z
                                    new Color4(1.0f, 1.0f, 1.0f, 1.0f)) // White colour
                            });
                        }
                        else
                        {
                            if (_bufferEndPosition[channelId] + i == _frameBufferLength)
                            {
                                // Reset stream position to the start of the channel
                                _vertexStream.Position = channelId * _frameBufferLength * VectorColor.SIZE_IN_BYTES;
                            }

                            _vertexStream.Write(
                                new VectorColor(
                                    new Vector3((_bufferEndPosition[channelId] + i) % _frameBufferLength, //X
                                        (channelId + 0.5f) * ((float) _targetHeight / CHANNEL_COUNT), //Y
                                        0.5f), // Z
                                    new Color4(1.0f, 1.0f, 1.0f, 1.0f))); // White colour
                        }
                    }

                    #endregion
                }
            }
            #endregion

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
            Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, Color4.Black);

            if (_isTemplateMatching)
            {
                for (int i = 0; i < CHANNEL_COUNT; i++)
                {
                    Device.ImmediateContext.Draw(
                        (int)(_bufferEndPosition[i] + 3 * _frameBufferLength / _targetWidth),
                        (int)(i * _frameBufferLength));
                }
            }
            else
            {
                for (int i = 0; i < CHANNEL_COUNT; i++)
                {
                    Device.ImmediateContext.Draw((int)_frameBufferLength, (int)(i * _frameBufferLength));
                }
            }

            _isReset = false;

#endregion
        }

        public void UpdateTimeSpan(int timeWindow)
        {
            _frameBufferLength = (uint)(_samplingRate * timeWindow / 1000);
            Array.Clear(_bufferEndPosition, 0, _bufferEndPosition.Length);
            // input initial values to buffer;
            Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, MapFlags.None, out _vertexStream);

            for (var channelId = 0; channelId < CHANNEL_COUNT; channelId++)
            {
                for (var i = 0; i < _frameBufferLength; i++)
                {
                    _vertexStream.Write(
                        new VectorColor(new Vector3(i, //X
                        (channelId + 0.5f) * ((float)_targetHeight / CHANNEL_COUNT), //Y
                        0.5f),  // Z
                        new Color4(1.0f, 1.0f, 1.0f, 1.0f)));    // White colour
                }
            }

            Device.ImmediateContext.UnmapSubresource(_vertices, 0);
        }

        public void UpdateVoltageRagne(int voltageRange)
        {
            _voltageRange = voltageRange;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            // NOTE: SharpDX 1.3 requires explicit Dispose() of everything
            Utilities.Dispose(ref _pConstantBuffer);
            Utilities.Dispose(ref _vertices);
            Utilities.Dispose(ref _vertexStream);
            Utilities.Dispose(ref _pPixelShader);
            Utilities.Dispose(ref _pVertexShader);
            base.Dispose(disposing);
            //Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
        }
    }
}
