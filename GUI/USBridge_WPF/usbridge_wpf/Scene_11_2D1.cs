﻿#if ONLY_IT_WOULD_WORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.WPF;

namespace USBridge_WPF
{
    public class Scene_11_2D1 : USBridge_WPF.SceneBase<D3D11_2D1>
    {
        Scene_11 sc = new Scene_11();
        SceneDwrite11 scw = new SceneDwrite11();

        public override D3D11_2D1 Renderer
        {
            set
            {
                base.Renderer = value;
                sc.Renderer = value;
                scw.Renderer = value;
            }
        }

        public override void RenderScene(RenderArgs args)
        {
            sc.RenderScene(args);
            scw.RenderScene(args);
        }
    }
}
#endif