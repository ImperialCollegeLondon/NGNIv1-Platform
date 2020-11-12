using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Direct3D;

namespace USBridge_WPF
{
    public class Scene_11 : USBridge_WPF.SceneBase<D3D11>
    {
        Buffer vertices;
        ShaderBytecode vertexShaderByteCode;
        VertexShader vertexShader;
        ShaderBytecode pixelShaderByteCode;
        PixelShader pixelShader;
        InputLayout layout;

        protected override void Attach()
        {
            if (Renderer == null)
                return;
            var device = Renderer.Device;
            var context = device.ImmediateContext;

            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("D:\\MiniTri.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
            pixelShaderByteCode = ShaderBytecode.CompileFromFile("D:\\MiniTri.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] { 
                new InputElement("POSITION",0,Format.R32G32B32A32_Float,0,0),
                new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0)
            });

            // Write vertex data to a datastream
            var stream = new DataStream(32 * 3, true, true);
            stream.WriteRange(new[]
                                  {
                                      new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4(0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
                                  });
            stream.Position = 0;

            // Instantiate Vertex buiffer from vertex data
            vertices = new Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 3,
                Usage = ResourceUsage.Default,
                StructureByteStride = 0
            });
            stream.Dispose();

            // Prepare All the stages
            context.InputAssembler.InputLayout = (layout);
            context.InputAssembler.PrimitiveTopology = (PrimitiveTopology.TriangleList);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 32, 0));
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
            //context.OutputMerger.SetTargets(Renderer.RenderView);
        }

        protected override void Detach()
        {
            Disposer.SafeDispose(ref vertexShaderByteCode);
            Disposer.SafeDispose(ref vertexShader);
            Disposer.SafeDispose(ref pixelShaderByteCode);
            Disposer.SafeDispose(ref pixelShader);
            Disposer.SafeDispose(ref vertices);
            Disposer.SafeDispose(ref layout);
        }

        public override void RenderScene(DrawEventArgs args)
        {
            Renderer.Device.ImmediateContext.ClearRenderTargetView(Renderer.RenderTargetView, new Color4(0.6f, 0, 0, 0));
            Renderer.Device.ImmediateContext.Draw(3, 0);
        }
    }
}
