using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
﻿using SharpDX.WPF;


namespace USBridge_WPF
{
    public abstract class SceneBase<T> : IDirect3D //, IDisposable
            where T : D3D
    {
        /// <summary>
        /// 
        /// </summary>
        private T m_context;

        /// <summary>
        /// 
        /// </summary>
        public virtual T Renderer
        {
            get { return m_context; }
            set
            {
                if (Renderer != null)
                {
                    Renderer.Rendering -= ContextRendering;
                    Detach();
                }
                m_context = value;
                if (Renderer != null)
                {
                    Renderer.Rendering += ContextRendering;
                    Attach();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public abstract void RenderScene(DrawEventArgs args);

        /// <summary>
        /// 
        /// </summary>
        //public abstract void Dispose();

        /// <summary>
        /// 
        /// </summary>
        protected abstract void Attach();

        /// <summary>
        /// 
        /// </summary>
        protected abstract void Detach();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        void IDirect3D.Reset(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Reset(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        void IDirect3D.Render(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Render(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aCtx"></param>
        /// <param name="args"></param>
        private void ContextRendering(object aCtx, DrawEventArgs args) { RenderScene(args); }


    }
}
