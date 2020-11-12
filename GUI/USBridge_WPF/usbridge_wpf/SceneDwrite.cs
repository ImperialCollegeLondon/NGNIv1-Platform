using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.WPF;
using ASIC_Interface;

namespace USBridge_WPF
{
  public class SceneDwrite : USBridge_WPF.SceneBase<SharpDX.WPF.D2D1>
	{
		TextFormat TextFormat;
		SolidColorBrush[] SceneColorBrush = new SolidColorBrush[128];
        SolidColorBrush StaticBrush;
        StrokeStyle DottedLine;
        StrokeStyleProperties DottedLineProperties;
        float[] dashes = new float[] { 10, 3 };
        //private RoundedRectangleGeometry rectangleGeometry;
        private readonly System.Diagnostics.Stopwatch RenderTimer = new System.Diagnostics.Stopwatch();
        private float layoutY = 0.0f, plotHeight=22.5f, plotterHeight=0.0f;
        private TextLayout textLayout;
        //bool labelPlotted = false;
        public FPS FPS { get; set; }


        public override SharpDX.WPF.D2D1 Renderer
        {
            set
            {
                base.Renderer = value;
            }
        }

		protected override void Attach()
		{

#region old method without using backbuffer 
            if (Renderer == null)
                return;

            //int targetWidth = Renderer.RenderTarget.Description.Width;
            //int targetHeight = Renderer.RenderTarget.Description.Height;
            //rectangleGeometry = new RoundedRectangleGeometry(Renderer.Factory, new RoundedRectangle() { RadiusX = 32, RadiusY = 32, Rect = new RectangleF(128, 128, targetWidth - 128, targetHeight - 128) });

            // Initial different colours
            Random r = new Random();

            // Disable Anti-aliased
            Renderer.RenderTarget2D.AntialiasMode = AntialiasMode.Aliased;
            for (int i = 0; i < 128; i++)
            {
                //SceneColorBrush[i] = new SolidColorBrush(Renderer.RenderTarget2D, new Color4(i*2/255.0f, (256-i*2)/255.0f, r.NextFloat(0,1), 1)); //new System.Drawing.Color. HSLColor(2.8 * i, 0.9 + 0.1 * r.NextDouble(), 0.9 + 0.1 * r.NextDouble()).ToArgb();
                SceneColorBrush[i] = new SolidColorBrush(Renderer.RenderTarget2D, r.NextColor());
            }

            // Initial static brush
            StaticBrush = new SolidColorBrush(Renderer.RenderTarget2D, new Color4(0, 0, 0, 1));
            DottedLineProperties = new StrokeStyleProperties()
            {
                DashStyle = DashStyle.Custom,
                LineJoin = LineJoin.Bevel,
            };
            DottedLine = new StrokeStyle(Renderer.RenderTarget2D.Factory, DottedLineProperties, dashes);

            // Initialize a TextFormat
            TextFormat = new TextFormat(Renderer.FactoryDW, "MyriadPro", 11)
            {
                //TextAlignment = TextAlignment.Leading,
                //ParagraphAlignment = ParagraphAlignment.Near,
            };

            Renderer.RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;
            textLayout = new TextLayout(Renderer.FactoryDW, "Hello Direct 2D", TextFormat, 400.0f, 200.0f);
            this.RenderTimer.Start();
#endregion

        }

		protected override void Detach()
		{
			Disposer.SafeDispose(ref TextFormat);
            //Disposer.SafeDispose(ref rectangleGeometry);

            for (int i = 0; i < 128; i++)
            {
                Disposer.SafeDispose(ref SceneColorBrush[i]);
            }

            Disposer.SafeDispose(ref StaticBrush);
            Disposer.SafeDispose(ref DottedLine);
            Disposer.SafeDispose(ref textLayout);
		}

		public override void RenderScene(DrawEventArgs args)
		{
            if (FPS != null)
            {
                FPS.AddFrame(args.TotalTime);
            }

            //float t = (float)args.Totaltime.Milliseconds * 0.001f;
            Renderer.RenderTarget2D.Clear(Color.White);
            //SceneColorBrush.Color = new Color4(1, 1, 1, (float)Math.Abs(Math.Cos(this.RenderTimer.ElapsedMilliseconds * .001)));
            float layoutYOffset = (float)Math.Cos(layoutY) * 50.0f;
            float point1 = 0, point2 = 0;
            int timetick;
            float timebase = 0;
        
            //Renderer.RenderTarget2D.DrawText("Hello Direct 2D", TextFormat, new SharpDX.RectangleF(0, 0, (float)args.RenderSize.Width, (float)args.RenderSize.Height), SceneColorBrush, DrawTextOptions.NoSnap);
            //Renderer.RenderTarget2D.FillGeometry(rectangleGeometry, SceneColorBrush, null);
            //Renderer.RenderTarget2D.DrawTextLayout(new Vector2(300, 330 + layoutYOffset), textLayout, SceneColorBrush, DrawTextOptions.NoSnap);
            //Renderer.RenderTarget2D.DrawLine(new Vector2(200, 400), new Vector2(200, 410), SceneColorBrush, 10.0f);
            //Renderer.RenderTarget2D.FillGeometry(new EllipseGeometry(Renderer.Factory, new Ellipse(new Vector2(200, 400), 10, 10)), SceneColorBrush, null);

            // Render X, Y axis

            if (USBridge_WPF.Realtime_Results.plotterWidth != 0)
            {
                timebase = USBridge_WPF.Realtime_Results.Tbase;
                plotHeight = USBridge_WPF.Realtime_Results.plotHeight;
                plotterHeight = USBridge_WPF.Realtime_Results.plotterHeight;

                generateAxis();

                for (int i = 0; i < 10; i++)
                {
                    // Label
                    Renderer.RenderTarget2D.DrawText(
                        ((timebase + USBridge_WPF.Realtime_Results.plotterWidth * 0.1f / USBridge_WPF.Realtime_Results.TZoom * i) * 0.0625).ToString("F2") + "ms",
                        TextFormat,
                        new SharpDX.RectangleF(USBridge_WPF.Realtime_Results.plotterWidth * 0.1f * i,
                            plotterHeight,
                            (float)args.RenderSize.Width,
                            (float)args.RenderSize.Height),
                        StaticBrush,
                        DrawTextOptions.None);
                    // Line
                    Renderer.RenderTarget2D.DrawLine(
                        new Vector2(USBridge_WPF.Realtime_Results.plotterWidth * 0.1f * i, 0),
                        new Vector2(USBridge_WPF.Realtime_Results.plotterWidth * 0.1f * i, plotterHeight),
                        StaticBrush, 0.1f);
                }

                # region Spike Plotting
                if ((FX3_IGLOOnano.Status)MainWindow.USBridge.status == FX3_IGLOOnano.Status.readOut_template_matching)
                {
                    // Show Spike data
                    //for (int i = 0; i < USBridge_WPF.Realtime_Results.spikes.Count; i++)
                    //{
                    //    if (USBridge_WPF.Realtime_Results.spikes[i].time > timebase + USBridge_WPF.Realtime_Results.plotterWidth / USBridge_WPF.Realtime_Results.TZoom)
                    //        // Any thing beyond this sample won't be displayed.
                    //        break;
                    //    else if (USBridge_WPF.Realtime_Results.spikes[i].time >= timebase)
                    //    {
                    //        // Within Display Scope
                    //        //Renderer.RenderTarget2D.FillEllipse(new Ellipse(new Vector2(USBridge_WPF.Realtime_Results.spikes[i].time * 50, (float)(USBridge_WPF.Realtime_Results.spikes[i].channelID * 13.9 + (USBridge_WPF.Realtime_Results.spikes[i].templateID + 1) * 4)), 2, 2), SceneColorBrush[USBridge_WPF.Realtime_Results.spikes[i].channelID * 4 + USBridge_WPF.Realtime_Results.spikes[i].templateID]);
                    //        Renderer.RenderTarget2D.FillRectangle(
                    //            new RectangleF(
                    //                (USBridge_WPF.Realtime_Results.spikes[i].time - timebase) * USBridge_WPF.Realtime_Results.TZoom,   // Each time step is 62.5us. Divided by 16 to downsample to 1ms per 1 point.
                    //                (float)((USBridge_WPF.Realtime_Results.spikes[i].channelID * 4 + USBridge_WPF.Realtime_Results.spikes[i].templateID) * plotHeight / 4),
                    //                1.0f,
                    //                plotHeight / 4),
                    //        SceneColorBrush[USBridge_WPF.Realtime_Results.spikes[i].channelID * 4 + USBridge_WPF.Realtime_Results.spikes[i].templateID]);
                    //    }
                    //}

                    /* For live view test */

                    for (int channelID=0;channelID<32;channelID++)
                    {

                    }
                }
                #endregion
                #region Raw Signal Plotting
                else if ((FX3_IGLOOnano.Status)MainWindow.USBridge.status == ASIC_Interface.FX3_IGLOOnano.Status.readOut_pass_through)
                {
                    // Show raw signal

                    /* For live view test */

                    for (int channelID=0;channelID<32;channelID++)
                    {
                        timetick = 0;
                        foreach (ushort datapoint in MainWindow.neuralChannel[0,channelID].rawSignal)
                        {
                            if (timetick == 0)
                            {
                                point2 = (plotHeight * (channelID + 1)) - (Convert.ToSingle(datapoint) * plotHeight / 512f);
                            }
                            else
                            {
                                point1 = point2;
                                point2 = (plotHeight * (channelID + 1)) - (Convert.ToSingle(datapoint) * plotHeight / 512f);
                                Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TZoom, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, point2), SceneColorBrush[channelID], 1.0f);
                                //Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TZoom, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, point2), StaticBrush, 1.0f);
                            }
                            timetick++;
                        }
                    }

                    /* Working Method */

                    //// Only display necessary points
                    //for (int channelID = 0; channelID < 32; channelID++)
                    //{
                    //    //Loop through all the channels
                    //    timetick = 0;
                    //    if (USBridge_WPF.Realtime_Results.rawsignal[0, channelID] != null && timebase <= USBridge_WPF.Realtime_Results.rawsignal[0, channelID].Count)
                    //    {
                    //        List<Int16> datapoints = USBridge_WPF.Realtime_Results.rawsignal[0, channelID].GetRange(
                    //        Convert.ToInt32(timebase),
                    //        Convert.ToInt32((timebase + USBridge_WPF.Realtime_Results.plotterWidth / USBridge_WPF.Realtime_Results.TZoom > USBridge_WPF.Realtime_Results.rawsignal[0, channelID].Count) ?    // Avoid out-of-range
                    //        USBridge_WPF.Realtime_Results.rawsignal[0, channelID].Count - timebase :
                    //        USBridge_WPF.Realtime_Results.plotterWidth / USBridge_WPF.Realtime_Results.TZoom
                    //        ));

                    //        foreach (Int16 datapoint in datapoints)
                    //        {
                    //            if (timetick == 0)
                    //            {
                    //                point2 = (plotHeight * (channelID + 1)) - (Convert.ToSingle(datapoint) * plotHeight / 512f);
                    //            }
                    //            else
                    //            {
                    //                point1 = point2;
                    //                point2 = (plotHeight * (channelID + 1)) - (Convert.ToSingle(datapoint) * plotHeight / 512f);
                    //                Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TZoom, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, point2), SceneColorBrush[channelID], 1.0f);
                    //                //Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TZoom, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, point2), StaticBrush, 1.0f);
                    //            }

                    //            //// Draw X-axis labels
                    //            //if (!labelPlotted && timetick % (int)(USBridge_WPF.Realtime_Results.plotterWidth * 0.1f / USBridge_WPF.Realtime_Results.TZoom) == 0)
                    //            //{
                    //            //    Renderer.RenderTarget2D.DrawText(((timetick + timebase) * 0.0625).ToString("F2") + "ms", TextFormat, new SharpDX.RectangleF(timetick * USBridge_WPF.Realtime_Results.TZoom, plotterHeight, (float)args.RenderSize.Width, (float)args.RenderSize.Height), StaticBrush, DrawTextOptions.None);
                    //            //    Renderer.RenderTarget2D.DrawLine(new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, 0), new Vector2(timetick * USBridge_WPF.Realtime_Results.TZoom, plotterHeight), StaticBrush, 0.1f);
                    //            //}

                    //            timetick++;
                    //        }

                    //        //if (!labelPlotted)
                    //        //    labelPlotted = true;

                    //        // Old methods: elementAt() is O(n) time complex.
                    //        //while (
                    //        //    timetick < USBridge_WPF.Realtime_Results.plotterWidth/USBridge_WPF.Realtime_Results.TScale 
                    //        //    && USBridge_WPF.Realtime_Results.rawsignal[0, channelID]!=null // Don't display channel without any signal
                    //        //    )
                    //        //{
                    //        //    // Display according to available areas
                    //        //    if (timetick == 0)
                    //        //    {
                    //        //        point2 = (22.5f * (channelID + 1)) - (Convert.ToSingle(USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick).ToString()) * 22.5f / 512f);
                    //        //        //point2 = 22.5f * (channelID + 1) - 11.25f - (
                    //        //        //    (((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x0100) == 0x0100) ?
                    //        //        //    (-((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x00FF) * 11.25f / 256f):
                    //        //        //    (((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x00FF) * 11.25f / 256f)
                    //        //        //    );
                    //        //    }
                    //        //    else
                    //        //    {
                    //        //        point1 = point2;
                    //        //        point2 = (22.5f * (channelID + 1)) - (Convert.ToSingle(USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick).ToString()) * 22.5f / 512f);
                    //        //        //point2 = 22.5f * (channelID + 1) - (USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick) * 22.5f / 512f);
                    //        //        //point2 = 22.5f * (channelID + 1) - 11.25f - (
                    //        //        //    (((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x0100) == 0x0100) ?
                    //        //        //    (-((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x00FF) * 11.25f / 256f) :
                    //        //        //    (((USBridge_WPF.Realtime_Results.rawsignal[0, channelID].ElementAt(timebase + timetick)) & 0x00FF) * 11.25f / 256f)
                    //        //        //    );
                    //        //        Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TScale, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TScale, point2), SceneColorBrush[channelID], 1.0f);
                    //        //        //Renderer.RenderTarget2D.DrawLine(new Vector2((timetick - 1) * USBridge_WPF.Realtime_Results.TScale, point1), new Vector2(timetick * USBridge_WPF.Realtime_Results.TScale, point2), StaticBrush, 1.0f);
                    //        //    }
                    //        //    timetick++;
                    //        //}
                    //    }
                    //}



                }
                #endregion
            }
        }

        private void generateAxis()
        {
            //throw new NotImplementedException();
            // Draw Frame Axis
            Renderer.RenderTarget2D.DrawLine(new Vector2(0, plotterHeight), new Vector2(USBridge_WPF.Realtime_Results.plotterWidth, plotterHeight), StaticBrush, 1.5f); //X
            Renderer.RenderTarget2D.DrawLine(new Vector2(0, 0), new Vector2(0, plotterHeight), StaticBrush, 1.5f);  // Y

            // Draw Dotted Lines#
            for (int i = 0; i < 32; i++)
            {
                Renderer.RenderTarget2D.DrawLine(new Vector2(0, i * plotHeight), new Vector2(USBridge_WPF.Realtime_Results.plotterWidth, i * plotHeight), StaticBrush, 0.2f, DottedLine);
            }


            // Draw Title
            //Renderer.RenderTarget2D.DrawText("Chnanel 1", TextFormat, new RectangleF(0, 0, 15f, 5f), StaticBrush, DrawTextOptions.NoSnap, MeasuringMode.Natural);
            // Draw Label
        }
	}
}
