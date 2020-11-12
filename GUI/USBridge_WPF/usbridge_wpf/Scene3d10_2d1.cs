using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.WPF;

namespace USBridge_WPF
{
    public class Scene3d10_2d1 : USBridge_WPF.SceneBase<D3D10>
    {
        public FPS FPS { get; set; }

        Scene sc3d = new Scene();
        //SceneDwrite sc2d = new SceneDwrite();

        public override D3D11 Renderer
        {
            set
            {
                base.Renderer = value;
                sc3d.Renderer = value;
                //sc2d.Renderer = value;
            }

        }

        protected override void Attach()
        {
            //throw new NotImplementedException();
        }

        protected override void Detach()
        {
            //throw new NotImplementedException();
        }

        public override void RenderScene(DrawEventArgs args) {
            if (FPS != null)
            {
                FPS.AddFrame(args.TotalTime);
            }
            /* implemented by individual scenes */
        }
    }
}
