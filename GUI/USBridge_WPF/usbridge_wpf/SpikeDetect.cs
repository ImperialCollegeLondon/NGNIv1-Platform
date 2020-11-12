using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace USBridge_WPF
{
    public class SpikeDetect
    {
        public static float[,][] SpikeBuffer = new float[32, 32][];
        readonly Stopwatch _stopWatch = new Stopwatch();

        public event SpikeScopeEventHandler OnDetection;

        public bool IsFinished { get; set; }

        public SpikeDetect(double samplingRate, uint address, uint channelId)
        {
            int depth = 30, spikeNum = 0;
            var bufferDataLength = (int)(Math.Floor(2 * samplingRate / 1000)); // Plot (-1ms, 1ms). Up to 30 superimposed spikes;

            var supraThresCnt = 0;
            var isDetected = false;

            SpikeBuffer[address, channelId] = new float[bufferDataLength * depth + 1]; //one extra position for holding the most recent spike number
            int bufferWriteIdx = 0;

            for (var i = 0; i < SpikeBuffer[address, channelId].Length; i++)
            {
                SpikeBuffer[address, channelId][i] = -1;
            }

            while (!IsFinished)
            {
                NeuralChannel.NeuralSignal datapoint;
                MainWindow.NeuralChannel[address, channelId].SpikeScopeFIFO.TryDequeue(out datapoint);
                var configuration = MainWindow.NeuralChannel[address, channelId].ChannelParameter;
                // If there is no data available. Sleep 1 ms
                if (datapoint == null)
                {
                    Thread.Sleep(2);
                    continue;
                }
                // Search for crossing event
                if (!isDetected &&
                    ((configuration.DetectTh >= 256 && datapoint.Amplitude > configuration.DetectTh) ||
                    (configuration.DetectTh < 256 && datapoint.Amplitude < configuration.DetectTh))
                    )
                {
                    supraThresCnt++;
                    if (supraThresCnt > 0)  // 4 consecutive samples across the threshold will be marked for spike event
                    {
                        isDetected = true;
                        //if (_selectedChannel != null && channelId == _selectedChannel.SequenceId)
                        //{
                        //    //_beepMemoryStream.Seek(0, SeekOrigin.Begin);
                        //    //using (SoundPlayer SP = new SoundPlayer(_beepMemoryStream))
                        //    //{
                        //    //    SP.PlaySync();
                        //    //}
                        //    _beepSoundPlayer.PlaySync();
                        //}
                        bufferWriteIdx = bufferDataLength * spikeNum + bufferDataLength / 2; // The peak is in the middle of the buffer
                    }
                }
                else
                {
                    supraThresCnt = 0;
                }

                if (isDetected)
                {
                    SpikeBuffer[address, channelId][bufferWriteIdx++] = datapoint.Amplitude;

                    if (bufferWriteIdx == bufferDataLength * spikeNum + bufferDataLength)   // Full buffer
                    {
                        SpikeBuffer[address, channelId][bufferDataLength * depth] = spikeNum;  // Update the most recent spike number

                        if (_stopWatch.IsRunning)
                        {
                            _stopWatch.Stop();
                            if (_stopWatch.ElapsedMilliseconds > 16)    // render every 16ms, limit the frame rate
                            {
                                // Plot
                                if (OnDetection != null)
                                    OnDetection.Invoke(this, new SpikeScopeEventArgs(address, channelId));
                                _stopWatch.Restart();
                            }
                            else
                                _stopWatch.Start();
                        }
                        else
                        {
                            if (OnDetection != null)
                                OnDetection.Invoke(this, new SpikeScopeEventArgs(address, channelId));
                            _stopWatch.Start();
                        }

                        //System.Threading.Thread.Sleep(16);  // To limit the frame rate

                        // Wait for the next spike
                        isDetected = false;
                        spikeNum = (spikeNum + 1) % depth;
                    }
                }
                else
                {
                    // move the points
                    for (int i = bufferDataLength * spikeNum; i < bufferDataLength * spikeNum + bufferDataLength / 2; i++)
                    {
                        SpikeBuffer[address, channelId][i] = SpikeBuffer[address, channelId][i + 1];
                    }
                    // push the last value
                    SpikeBuffer[address, channelId][bufferDataLength * spikeNum + bufferDataLength / 2 - 1] = datapoint.Amplitude;
                }
            }
        }
    }
}
