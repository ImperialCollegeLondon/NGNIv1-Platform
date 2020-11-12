using System;
using System.Threading.Tasks.Dataflow;
using ASIC_Interface;

namespace USBridge_WPF
{
    /// <summary>
    /// 
    /// </summary>
    class SyntheticSignal
    {
        public ITargetBlock<byte[]> InputDataFlow;
        private static int _numDataStreams;
        private static readonly RandomNumber Random = new RandomNumber();

        private static double[,] _synthEcgAmplitude;
        private static double[,,] _synthSpikeAmplitude;
        private static double[,,] _synthSpikeDuration;
        private static double[,] _synthRelativeSpikeRate;
        private static double[,,] _amplifierPreFilter;
        private static ushort[,] _synthSpikeEvent;


        private const int SAMPLES_PER_DATA_BLOCK = 60;
        private const int MAX_NUM_BLOCKS = 120;

        private bool _isRunning;
        //private long _inputDataLength;

        //public SyntheticSignal()
        //{
        //    this.initInputDataFlow();
        //}
        private static void LoadSyntheticData(int numBlocks, double sampleRate)
        {
            int t, channel, stream;

            var tStepMsec = 1000.0 / sampleRate;
            double spikeDelay=0;

            // Other synthetic waveform variables.
            var tPulse = 0.0;

            // If the sample rate is 5 kS/s or higher, generate sythetic neural data;
            // otherwise, generate synthetic ECG data.
            if (sampleRate > 4999.9)
            {
                // Generate synthetic neural data.num
                int block;
                for (block = 0; block < numBlocks; ++block)
                {
                    for (stream = 0; stream < _numDataStreams; ++stream)
                    {
                        for (channel = 0; channel < 32; ++channel)
                        {
                            var spikePresent = false;
                            var spikeNum = 0;
                            if (Random.randomUniform() < _synthRelativeSpikeRate[stream, channel] * tStepMsec)
                            {
                                spikePresent = true;
                                spikeDelay = Random.randomUniform(0.0, 0.3); // add some random time jitter
                                var templateRnd = Random.randomUniform();
                                if (templateRnd < 0.25)
                                    spikeNum = 1; // choose between one of four spike types
                                else if (templateRnd >= 0.25 && templateRnd < 0.5)
                                    spikeNum = 2;
                                else if (templateRnd >= 0.5 && templateRnd < 0.75)
                                    spikeNum = 3;
                            }
                            if (MainWindow.IsSpikeSorting)
                            {
                                for (t = 0; t < SAMPLES_PER_DATA_BLOCK; ++t)
                                {
                                    // Synthesis spike events
                                    if (spikePresent)
                                    {
                                        // Create synthetic spike event only
                                        _synthSpikeEvent[stream, channel] =
                                            (ushort)
                                                (
                                                    ((channel << 5) &
                                                     (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Channel_Mask) |
                                                    ((ushort) (SAMPLES_PER_DATA_BLOCK * block + t + spikeDelay / tStepMsec) &
                                                     (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Spike_Time_Mask) |
                                                    ((spikeNum << 10) &
                                                     (ushort) FX3_IGLOO_Nano.SpikeSignalMask.Template_Mask)
                                                    );
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // Synthesis waveform/event
                                for (t = 0; t < SAMPLES_PER_DATA_BLOCK; ++t)
                                {
                                    // Create realistic background Gaussian noise of 2.4 uVrms (would be more in cortex)
                                    _amplifierPreFilter[stream, channel, SAMPLES_PER_DATA_BLOCK * block + t] =
                                        5 * Random.randomGaussian(); //Change to 5uV.
                                    if (spikePresent)
                                    {
                                        // Create synthetic spike
                                        if (t * tStepMsec > spikeDelay &&
                                            t * tStepMsec <
                                            _synthSpikeDuration[stream, channel, spikeNum] + spikeDelay)
                                        {
                                            _amplifierPreFilter[stream, channel, SAMPLES_PER_DATA_BLOCK * block + t]
                                                +=
                                                _synthSpikeAmplitude[stream, channel, spikeNum] *
                                                Math.Exp(-2.0 * (t * tStepMsec - spikeDelay)) *
                                                Math.Sin(Math.PI * 2 * (t * tStepMsec - spikeDelay) /
                                                         _synthSpikeDuration[stream, channel, spikeNum]);
                                        }
                                    }
                                    // generate digitised value
                                    _amplifierPreFilter[stream, channel, SAMPLES_PER_DATA_BLOCK * block + t] =
                                        _amplifierPreFilter[stream, channel, SAMPLES_PER_DATA_BLOCK * block + t] / 1 +
                                        256;
                                }
                            }
                        }
                        _synthSpikeEvent[stream, 32] = 0xC000;
                    }
                }
            }
            else 
            {
                // Generate synthetic ECG data.
                for (t = 0; t < SAMPLES_PER_DATA_BLOCK * numBlocks; ++t) {
                    // Piece together half sine waves to model QRS complex, P wave, and T wave
                    double ecgValue;
                    if (tPulse < 80.0) {
                        ecgValue = 40.0 * Math.Sin(Math.PI * 2 * tPulse / 160.0); // P wave
                    } else if (tPulse > 100.0 && tPulse < 120.0) {
                        ecgValue = -250.0 * Math.Sin(Math.PI * 2 * (tPulse - 100.0) / 40.0); // Q
                    } else if (tPulse > 120.0 && tPulse < 180.0) {
                        ecgValue = 1000.0 * Math.Sin(Math.PI * 2 * (tPulse - 120.0) / 120.0); // R
                    } else if (tPulse > 180.0 && tPulse < 260.0) {
                        ecgValue = -120.0 * Math.Sin(Math.PI * 2 * (tPulse - 180.0) / 160.0); // S
                    } else if (tPulse > 340.0 && tPulse < 400.0) {
                        ecgValue = 60.0 * Math.Sin(Math.PI * 2 * (tPulse - 340.0) / 120.0); // T wave
                    } else {
                        ecgValue = 0.0;
                    }
                    for (stream = 0; stream < _numDataStreams; ++stream) {
                        for (channel = 0; channel < 32; ++channel) {
                            // Multiply basic ECG waveform by channel-specific amplitude, and
                            // add 2.4 uVrms noise.
                            _amplifierPreFilter[stream,channel,t] =
                                    _synthEcgAmplitude[stream,channel] * ecgValue +
                                    2.4 * Random.randomGaussian();
                        }
                    }
                    tPulse += tStepMsec;
                }

                // Repeat ECG waveform with regular period.
                if (tPulse > 840.0) tPulse = 0.0;
            }
        }

        public void InitInputDataFlow(ITargetBlock<byte[]> targetDataflow, int numStreams)
        {
            InputDataFlow = targetDataflow;
            _numDataStreams = numStreams;


            _synthEcgAmplitude = new double[_numDataStreams,32];
            _synthSpikeAmplitude = new double[_numDataStreams, 32, 4];
            _synthSpikeDuration = new double[_numDataStreams, 32, 4];
            _synthRelativeSpikeRate = new double[_numDataStreams, 32];
            _amplifierPreFilter = new double[_numDataStreams, 32, SAMPLES_PER_DATA_BLOCK * MAX_NUM_BLOCKS];
            _synthSpikeEvent = new ushort[_numDataStreams, 33];

            // Assign random parameters for synthetic waveforms.
            for (int stream = 0; stream < _numDataStreams; ++stream)
            {
                for (int channel = 0; channel < 32; ++channel)
                {
                    _synthEcgAmplitude[stream,channel] = Random.randomUniform(0.5, 3.0);
                    for (int spikeNum = 0; spikeNum < 4; ++spikeNum)
                    {
                        _synthSpikeAmplitude[stream, channel, spikeNum] =
                            Random.randomUniform(-250.0, 100.0);
                        _synthSpikeDuration[stream,channel,spikeNum] =
                                Random.randomUniform(0.3, 1.7);
                        _synthRelativeSpikeRate[stream,channel] =
                                Random.randomUniform(0.1, 5.0);
                    }
                }
            }

            _isRunning = true;
        }

        public void StopInputDataFlow()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Start signal generator thread.
        /// </summary>
        public async void StartInputDataFlow()
        {
            // Setup
            //List<ushort> data = new List<ushort>();
            const int numBlocks = 1;
            var data = new byte[2 * SAMPLES_PER_DATA_BLOCK * numBlocks * 32 * _numDataStreams];

            while(_isRunning)
            {
                LoadSyntheticData(numBlocks, MainWindow.INTAN[MainWindow.CurrentDeviceAddress].SamplingRate);
                var i = 0;
                if (MainWindow.IsSpikeSorting)
                {
                    for (ushort channel = 0; channel < 33; channel++ )
                    {
                        for (ushort stream = 0; stream < _numDataStreams; stream++)
                        {
                            if (_synthSpikeEvent[stream, channel] != 0)
                            {
                                // Load spike data
                                data[i++] = (byte) (_synthSpikeEvent[stream, channel] & 0xFF);
                                data[i++] = (byte) ((_synthSpikeEvent[stream, channel] & 0xFF00) >> 8);
                            }
                        }
                    }
                    //// 2ms tick
                    //data[i++] = 0x00;
                    //data[i] = 0xC0;
                }
                else
                {
                    for (var t = 0; t < SAMPLES_PER_DATA_BLOCK * numBlocks; t++)
                    {
                        for (ushort channel = 0; channel < 32; channel++)
                        {
                            for (ushort stream = 0; stream < _numDataStreams; stream++)
                            {
                                //data.Add((ushort)(channel & 0x1F | (Convert.ToUInt16(amplifierPreFilter[stream, channel, t]) & 0x1FF) << 5));
                                var tmp =
                                    (ushort)
                                        ((channel & 0x1F) << 9) |
                                            (Convert.ToUInt16(_amplifierPreFilter[stream, channel, t]) & 0x1FF);
                                data[i++] = (byte)((tmp & 0xFF));
                                data[i++] = (byte)((tmp & 0xFF00) >> 8);
                                //inputDataFlow.Post(BitConverter.GetBytes((ushort)(channel & 0x1F | (Convert.ToUInt16(amplifierPreFilter[stream, channel, t]) & 0x1FF) << 5)).ToArray());
                                //System.Threading.Thread.Sleep(1 / (15000 * 32));
                            }
                        }
                    }
                }
                //await inputDataFlow.SendAsync(data.SelectMany(i => BitConverter.GetBytes(i)).ToArray());
                await InputDataFlow.SendAsync(data);
                //inputDataFlow.Post(data.SelectMany(i => BitConverter.GetBytes(i)).ToArray());
                //System.Threading.Thread.Sleep(1000);
            }

            // Finish
            InputDataFlow.Complete();
        }
    }
}
