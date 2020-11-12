using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.WPF;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D10.Buffer;
using Device = SharpDX.Direct3D10.Device;
using System.ComponentModel;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;

namespace USBridge_WPF
{
        /// <summary>
    /// 
    /// </summary>
    //[StructLayout(LayoutKind.Sequential, Pack = 4)]
    //public struct VectorColor
    //{
    //    public VectorColor(Vector3 p, Color4 c)
    //    {
    //        Point = p;
    //        Color = c;
    //    }

    //    public Vector3 Point;

    //    public Color4 Color;

    //    public const int SizeInBytes = (3 + 4) * 4;
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    //[StructLayout(LayoutKind.Sequential, Pack = 4)]
    //public struct Projections
    //{
    //    public Matrix World;

    //    public Matrix View;

    //    public Matrix Projection;
    //}
    public class Scene : USBridge_WPF.SceneBase<D3D10>
    {
        public FPS FPS { get; set; }

        private InputLayout VertexLayout;
        private DataStream VertexStream;
        private Buffer Vertices;
        private Effect SimpleEffect;
        private Color4 OverlayColor = new Color4(1.0f);
        private Random rnd = new Random();

        private int sizeInBytes;

        protected override void Attach()
        {
            Device device = Renderer.Device;
            if (device == null)
                return;

            ShaderBytecode shaderBytes = ShaderBytecode.CompileFromFile("D:\\MyShader.fx", "fx_4_0", ShaderFlags.None, EffectFlags.None, null, null);
            this.SimpleEffect = new Effect(device, shaderBytes);

            EffectTechnique technique = this.SimpleEffect.GetTechniqueByIndex(0); ;
            EffectPass pass = technique.GetPassByIndex(0);

            this.VertexLayout = new InputLayout(device, pass.Description.Signature, new[] {
                    new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0) 
                });

            sizeInBytes = sizeof(int) * 1 * 1000;
            //sizeInBytes = 32 * 6;

            this.VertexStream = new DataStream(sizeInBytes, true, true);
            //this.VertexStream.WriteRange(new[] {
            //    new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    new Vector4(0.5f, 1, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            //    new Vector4(1.5f, 0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            //    new Vector4(2f, 1f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            //    new Vector4(2.5f, 0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            //    new Vector4(3f, 1f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    //new Vector4(1.0f, 0.5f, 0, 1.0f),    new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    //new Vector4(1.5f, 1f, 0, 1.0f),   new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    //new Vector4(2.0f, 0.5f, 0, 1.0f),    new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    //new Vector4(2.5f, 0.5f, 0, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            //    //new Vector4(3.0f, 1.5f, 0, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            //    //new Vector4(3.5f, 2.5f, 0, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
            //    });
            //this.VertexStream.Position = 0;

            //for (int i = 0; i < Vertices.Length; i++)
            //{
                this.Vertices = new Buffer(device, this.VertexStream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = sizeInBytes,
                    Usage = ResourceUsage.Dynamic
                }
                );
            //}
            device.Flush();
        }

        protected override void Detach()
        {
            //for (int i = 0; i < Vertices.Length;i++ )
                Vertices.Dispose();
            VertexLayout.Dispose();
            SimpleEffect.Dispose();
            VertexStream.Dispose();
        }

        public override void RenderScene(DrawEventArgs args)
        {
            if (FPS != null)
                FPS.AddFrame(args.TotalTime);

            Device device = Renderer.Device;
            if (device == null)
                return;

            float t = (float)args.TotalTime.Milliseconds * 0.001f;
            this.OverlayColor.Alpha = 1.0f;

            device.InputAssembler.InputLayout = (this.VertexLayout);
            device.InputAssembler.PrimitiveTopology = (SharpDX.Direct3D.PrimitiveTopology.LineStrip);
            device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(this.Vertices, 32, 0));
            device.OutputMerger.SetDepthStencilState(new DepthStencilState(device, new DepthStencilStateDescription()
                {
                    IsDepthEnabled = false
                }), 1);



            var Camera = Renderer.Camera;
            Camera = new SharpDX.WPF.Cameras.FirstPersonCamera();
            Camera.SetViewParams(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            //Camera.SetProjParams((float)Math.PI / 2, targetWidth / targetHeight, 0.01f, 100.0f);
            //Camera.SetViewParams(new Vector3(targetWidth / 2, targetHeight * 31f / 32f, -5), new Vector3(targetWidth / 2, targetHeight * 31f / 32f, 0), Vector3.UnitY);
            //Camera.SetViewParams(new Vector3(0, targetHeight * 31f / 32f, -5), new Vector3(0, targetHeight * 31f / 32f, 0), Vector3.UnitY);
            //Camera.SetScalers(0, 0.1f);

            //Camera.Projection.Orthogonalize();
            //var view = Matrix.LookAtLH(new Vector3(0, 0, -50), new Vector3(0, 0, 0), Vector3.UnitY);
            //var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, targetWidth / (float)targetHeight, 0.1f, 100.0f);


            EffectTechnique technique = this.SimpleEffect.GetTechniqueByIndex(0);
            EffectPass pass = technique.GetPassByIndex(0);

            EffectVectorVariable overlayColor = this.SimpleEffect.GetVariableBySemantic("OverlayColor").AsVector();
            EffectMatrixVariable xWorld = this.SimpleEffect.GetVariableByName("xWorld").AsMatrix();
            EffectMatrixVariable xView = this.SimpleEffect.GetVariableByName("xView").AsMatrix();
            EffectMatrixVariable xProjection = this.SimpleEffect.GetVariableByName("xProjection").AsMatrix();

            int targetWidth = this.Renderer.RenderTarget.Description.Width;
            int targetHeight = this.Renderer.RenderTarget.Description.Height;

            //var g_World = Matrix.RotationY(DXUtils.DEG2RAD((float)args.TotalTime.TotalSeconds * 15));
            //var g_World = Matrix.Multiply(Matrix.Translation(0, targetHeight * 31 / 32, 0), Matrix.Scaling(1));
            var g_World = Matrix.Multiply(Matrix.Scaling(1), Matrix.Translation(-targetWidth/2, -targetHeight/2, 0));
            overlayColor.Set(this.OverlayColor);
            xWorld.SetMatrix(g_World);
            //xView.SetMatrix(Matrix.LookAtLH(new Vector3(5, 5, -0.5f), new Vector3(5, 5, 0), Vector3.UnitY));
            xView.SetMatrix(Camera.View);
            //xProjection.SetMatrix(Camera.Projection);
            xProjection.SetMatrix(Matrix.OrthoLH(targetWidth, targetHeight, 0.01f, 100f));

            //int width = (int)USBridge_WPF.Realtime_Results.plotterWidth;
            //int height = (int)USBridge_WPF.Realtime_Results.plotHeight;

            //device.Rasterizer.SetViewports(new Viewport(0, 0, targetWidth, targetHeight, 0, 1.0f));
            



            device.ClearRenderTargetView(Renderer.RenderTargetView, new Color4(1.0f, 0, 0, 0));
            for (int i = 0; i < technique.Description.PassCount; ++i)
            {
                pass.Apply();
                using (var stream = this.Vertices.Map(MapMode.WriteDiscard, SharpDX.Direct3D10.MapFlags.None))
                {
                        for (int j = 0; j < 1000; j++)
                        {
                            stream.WriteRange(new[] { new Vector4((float)(j % 1000), 1 + (j / 1000) * (targetHeight / 32) + rnd.NextFloat(0f, targetHeight / 32), 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f) });
                        }
                    //stream.Position = 0;
                    //stream.WriteRange(new[] {
                    //    new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    new Vector4(0.5f, 1, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                    //    new Vector4(1.5f, 0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                    //    new Vector4(2f, 1f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                    //    new Vector4(2.5f, 0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                    //    new Vector4(3f, 1f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    //new Vector4(1.0f, 0.5f, 0, 1.0f),    new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    //new Vector4(1.5f, 1f, 0, 1.0f),   new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    //new Vector4(2.0f, 0.5f, 0, 1.0f),    new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    //new Vector4(2.5f, 0.5f, 0, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    //    //new Vector4(3.0f, 1.5f, 0, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                    //    //new Vector4(3.5f, 2.5f, 0, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
                    //    });
                }

                this.Vertices.Unmap();
                device.Draw(1000, 0);
e                //for (int j = 0; j < 4; j++)
                //    device.Draw(1000, j * 1000);
            }
        }

    }
}
