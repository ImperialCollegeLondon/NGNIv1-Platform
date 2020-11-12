using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace USBridge_WPF
{
    public class SpikeScope : D3D11
    {

        private VertexShader m_pVertexShader;
        private PixelShader m_pPixelShader;
        private ConstantBuffer<Projections> m_pConstantBuffer;
        private Matrix m_Projection;
        private static int depth = 30;  //Up to 30 superimposed spikes;
        private Buffer _vertices;
        private DataStream _vertexStream;
        private int currentBufferIdx = 0;

        private int targetWidth, _targetHeight;
        private readonly bool _isSpikeSorting;
        private int _bufferDataLength;
        private float[] historyBuffer;
        private bool isDetected;

        private uint _address;
        private uint _channelId;
        private int streamEndPosition = 0;
        private readonly Color4 _backgroundColor4 = Color4.Black;

        public SpikeScope(double samplingRate, double plotterWidth, double plotterHeight, bool isSpikeSorting, uint address , uint channel)
        {
            _targetHeight = (int) plotterHeight;
            _isSpikeSorting = isSpikeSorting;
            _address = address;
            targetWidth = (int) plotterWidth;
            //this.streamID = MainWindow.CurrentDeviceAddress;
            _channelId = channel;

            _bufferDataLength = (int)(Math.Floor(2 * samplingRate / 1000)); // Plot (-1ms, 1ms). 

            using (var dg = new DisposeGroup())
            {
                // --- init shaders
                ShaderFlags sFlags = ShaderFlags.EnableStrictness;
#if DEBUG
                sFlags |= ShaderFlags.Debug;
#endif
                //var pVSBlob = dg.Add(ShaderBytecode.CompileFromFile("D:\\MyShader.fx", "VS", "vs_4_0", sFlags, EffectFlags.None));
                var pVSBlob = dg.Add(ShaderBytecode.Compile(USBridge_WPF.Properties.Resources.MyShader, "VS", "vs_4_0", sFlags, EffectFlags.None));
                var inputSignature = dg.Add(ShaderSignature.GetInputSignature(pVSBlob));
                m_pVertexShader = new VertexShader(Device, pVSBlob);

                //var pPSBlob = dg.Add(ShaderBytecode.CompileFromFile("D:\\MyShader.fx", "PS", "ps_4_0", sFlags, EffectFlags.None));
                var pPSBlob = dg.Add(ShaderBytecode.Compile(USBridge_WPF.Properties.Resources.MyShader, "PS", "ps_4_0", sFlags, EffectFlags.None));
                m_pPixelShader = new PixelShader(Device, pPSBlob);

                // --- let DX know about the pixels memory layout
                var layout = new InputLayout(Device, inputSignature, new[]{
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                });
                Device.ImmediateContext.InputAssembler.InputLayout = (layout);
                dg.Add(layout);

                //Device.ImmediateContext.OutputMerger.SetDepthStencilState(new DepthStencilState(Device, new DepthStencilStateDescription()
                //{
                //    IsDepthEnabled = false
                //}), 1);

                // --- init vertices
                // 

                //int BufferSize = VectorColor.SizeInBytes * ChannelCount * Datalength;
                int bufferSize = VectorColor.SIZE_IN_BYTES * _bufferDataLength * depth;
                this._vertexStream = new DataStream(bufferSize, true, true);
                this._vertices = new Buffer(Device, this._vertexStream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = bufferSize,
                    Usage = ResourceUsage.Dynamic,
                }
                );
                //for (int i = 0; i < depth; i++)
                //{
                //    this.Vertices[i] = new Buffer(Device, this.VertexStream, new BufferDescription()
                //    {
                //        BindFlags = BindFlags.VertexBuffer,
                //        CpuAccessFlags = CpuAccessFlags.Write,
                //        OptionFlags = ResourceOptionFlags.None,
                //        SizeInBytes = BufferSize,
                //        Usage = ResourceUsage.Dynamic,
                //    }
                //    );
                //}

                //Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(Vertices, VectorColor.SizeInBytes, 0));
                Device.ImmediateContext.InputAssembler.PrimitiveTopology = (PrimitiveTopology.LineStrip);

                // --- create the constant buffer
                m_pConstantBuffer = new ConstantBuffer<Projections>(Device);
                Device.ImmediateContext.VertexShader.SetConstantBuffer(0, m_pConstantBuffer.Buffer);
            }

            Camera = new SharpDX.WPF.Cameras.FirstPersonCamera();
            //Camera.SetProjParams((float)Math.PI / 2, 1, 0.01f, 100.0f);
            Camera.SetViewParams(new Vector3(0.0f, 0.0f, -5.0f), new Vector3(0.0f, 0.0f, 0.0f), Vector3.UnitY);

            // input initial values to buffer;
            Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, MapFlags.None, out _vertexStream);
            for (var i = 0; i < _bufferDataLength*depth; i++)
            {
                _vertexStream.Write(
                    new VectorColor(new Vector3(i, //X
                        0.5f * ((float)_targetHeight), //Y
                        0.5f), // Z
                        _backgroundColor4));
            }

            Device.ImmediateContext.UnmapSubresource(_vertices, 0);
        }
        public override void Reset(int w, int h)
        {
            base.Reset(w, h);
            m_Projection = Matrix.OrthoLH(_bufferDataLength, _targetHeight, 0f, 100f);
        }

        public override void RenderScene(DrawEventArgs args)
        {
            int newSpikeNum, recentSpikesStart;
            int plotLength;
            bool acrossEnd = false;
            if (SpikeScopeView.SpikeBuffer[_address, _channelId] != null)
            {

                var g_World = Matrix.Translation(-_bufferDataLength / 2, -_targetHeight / 2, 0);

                //
                // Update variables
                //
                m_pConstantBuffer.Value = new Projections
                {
                    World = Matrix.Transpose(g_World),
                    View = Matrix.Transpose(Camera.View),
                    Projection = Matrix.Transpose(m_Projection),
                };

                //
                // Update vertices
                //
                Device.ImmediateContext.MapSubresource(_vertices, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out _vertexStream);

                plotLength = SpikeScopeView.SpikeBuffer[_address, _channelId].Length - 1;

                //// emulate CRT trace retention
                //newSpikeNum = (int)Channel_Config.spikeBuffer[streamID, channelID][plotLength];

                //if (newSpikeNum < depth / 3)
                //    acrossEnd = true;

                //recentSpikesStart = acrossEnd ? (depth + newSpikeNum - depth / 3) : (newSpikeNum - (depth / 3));
                //for (int idx = 0; idx < plotLength; idx++)
                //{
                //    int i = (recentSpikesStart * bufferDataLength + idx) % plotLength;
                //    int spikePlotCnt = (idx / bufferDataLength);
                //    if (spikePlotCnt < depth / 3)
                //    {
                //        // Blue
                //        VertexStream.Write(new VectorColor(new Vector3((VertexStream.Position / VectorColor.SizeInBytes) % bufferDataLength, (Channel_Config.spikeBuffer[streamID, channelID][i] / 512f) * targetHeight, 0.5f), new Color4(0.0f, 0.0f, 1.0f, 1.0f)));
                //        //VertexStream.Write(new VectorColor(new Vector3((VertexStream.Position / VectorColor.SizeInBytes) % bufferDataLength, ((VertexStream.Position / VectorColor.SizeInBytes) % bufferDataLength / 512f) * targetHeight, 0.5f), new Color4(0.0f, 0.0f, 1.0f, 1.0f)));
                //    }
                //    else if ((spikePlotCnt >= depth / 3) && (spikePlotCnt < (depth * 2) / 3))
                //    {
                //        // Gray
                //        VertexStream.Write(new VectorColor(new Vector3((VertexStream.Position / VectorColor.SizeInBytes) % bufferDataLength, (Channel_Config.spikeBuffer[streamID, channelID][i] / 512f) * targetHeight, 0.6f), new Color4(0.5f, 0.5f, 0.5f, 1.0f)));
                //    }
                //    else
                //    {
                //        // Light Gray
                //        VertexStream.Write(new VectorColor(new Vector3((VertexStream.Position / VectorColor.SizeInBytes) % bufferDataLength, (Channel_Config.spikeBuffer[streamID, channelID][i] / 512f) * targetHeight, 0.7f), new Color4(0.75f, 0.75f, 0.75f, 1.0f)));
                //    }
                //}

                // All blue traces
                for (int idx = 0; idx < plotLength; idx++)
                    _vertexStream.Write(
                        new VectorColor(
                            new Vector3(idx%_bufferDataLength,
                                (float) ((SpikeScopeView.SpikeBuffer[_address, _channelId][idx] / MainWindow.INTAN[_address].FullRangeBit) * _targetHeight), 0.5f),
                            new Color4(WaveformScope.ColourValues[_channelId], 1.0f)));

                // Make sure it is unmapped.
                Device.ImmediateContext.UnmapSubresource(_vertices, 0);

                //
                // Configure GPU pipeline
                //
                Device.ImmediateContext.VertexShader.Set(m_pVertexShader);
                Device.ImmediateContext.VertexShader.SetConstantBuffer(0, m_pConstantBuffer.Buffer);
                Device.ImmediateContext.PixelShader.Set(m_pPixelShader);
                Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices, VectorColor.SIZE_IN_BYTES, 0));

                //
                // Draw
                //

                // Clear the back buffer
                Device.ImmediateContext.ClearRenderTargetView(this.RenderTargetView, _backgroundColor4);
                //Device.ImmediateContext.Draw(bufferDataLength * depth, 0);
                for (int i = 0; i < depth; i++)
                {
                    Device.ImmediateContext.Draw(_bufferDataLength, i * _bufferDataLength);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // NOTE: SharpDX 1.3 requires explicit Dispose() of everything
            Utilities.Dispose(ref m_pVertexShader);
            Utilities.Dispose(ref m_pPixelShader);
            Utilities.Dispose(ref m_pConstantBuffer);
            Utilities.Dispose(ref _vertices);
            Utilities.Dispose(ref _vertexStream);
        }

    }
}
