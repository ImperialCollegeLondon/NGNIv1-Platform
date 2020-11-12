using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace INTAN_RHD2000
{
    public class RHD2132 : INotifyPropertyChanged
    {
        /// <summary>
        /// Constructor.  Set sampling rate variable to 16.0 kS/s/channel
        /// </summary>

        private Rhd2000Registers _configuration;
        public Rhd2000Registers Configuration {
            get { return _configuration; }
            set {
                if (value == _configuration) return;
                _configuration = value;
                NotifyPropertyChange("Configuration");
            }
        }

        #region sampling rate
        private int _samplingRate;
        public int SamplingRate
        {
            get { return _samplingRate; }
            set
            {
                if (value == _samplingRate) return;
                _samplingRate = value;
                _configuration.DefineSampleRate(value); //define the adc buffer bias and mux bias
                DSPCutoffList = null;  //update dsp frequency list.
                NotifyPropertyChange("SamplingRate");
            }
        }

        private static ObservableCollection<SamplingRate> _samplingRateList;
        public static ObservableCollection<SamplingRate> SamplingRateList
        {
            get
            {
                return _samplingRateList ?? (_samplingRateList = new ObservableCollection<SamplingRate>()
                {
                    new SamplingRate() {Value = 1000, Disp = "1.0 kS/s"},
                    new SamplingRate() {Value = 1250, Disp = "1.25 kS/s"},
                    new SamplingRate() {Value = 1500, Disp = "1.5 kS/s"},
                    new SamplingRate() {Value = 2000, Disp = "2.0 kS/s"},
                    new SamplingRate() {Value = 2500, Disp = "2.5 kS/s"},
                    new SamplingRate() {Value = 3000, Disp = "3.0 kS/s"},
                    new SamplingRate() {Value = 3330, Disp = "3.33 kS/s"},
                    new SamplingRate() {Value = 4000, Disp = "4.0 kS/s"},
                    new SamplingRate() {Value = 5000, Disp = "5.0 kS/s"},
                    new SamplingRate() {Value = 6250, Disp = "6.25 kS/s"},
                    new SamplingRate() {Value = 8000, Disp = "8.0 kS/s"},
                    new SamplingRate() {Value = 10000, Disp = "10 kS/s"},
                    new SamplingRate() {Value = 12500, Disp = "12.5 kS/s"},
                    new SamplingRate() {Value = 15000, Disp = "15 kS/s"},
                    // Following requires tune up FPGA CLK via reprogramming
                    new SamplingRate() {Value = 20000, Disp = "20 kS/s"},
                    new SamplingRate() {Value = 25000, Disp = "25 kS/s"},
                    new SamplingRate() {Value = 30000, Disp = "30 kS/s"},
                });
            }
        }
        #endregion sampling rate

        #region lower bandwidth
        private double _lowerBw;
        public double LowerBw
        {
            get { return _lowerBw; }
            set
            {
                if (!(Math.Abs(value - _lowerBw) > 0.001)) return;
                _lowerBw = value;
                _configuration.SetLowerBandwidth(value);
                NotifyPropertyChange("LowerBW");
            }
        }

        private ObservableCollection<LowerBw> _lowerBwList;
        public ObservableCollection<LowerBw> LowerBwList
        {
            get
            {
                return _lowerBwList ?? (_lowerBwList = new ObservableCollection<LowerBw>()
                {
                    new LowerBw() {Value = 500, Disp = "500 Hz"},
                    new LowerBw() {Value = 300, Disp = "300 Hz"},
                    new LowerBw() {Value = 250, Disp = "250 Hz"},
                    new LowerBw() {Value = 200, Disp = "200 Hz"},
                    new LowerBw() {Value = 150, Disp = "150 Hz"},
                    new LowerBw() {Value = 100, Disp = "100 Hz"},
                    new LowerBw() {Value = 75, Disp = "75 Hz"},
                    new LowerBw() {Value = 50, Disp = "50 Hz"},
                    new LowerBw() {Value = 30, Disp = "30 Hz"},
                    new LowerBw() {Value = 25, Disp = "25 Hz"},
                    new LowerBw() {Value = 15, Disp = "15 Hz"},
                    new LowerBw() {Value = 10, Disp = "10 Hz"},
                    new LowerBw() {Value = 7.5, Disp = "7.5 Hz"},
                    new LowerBw() {Value = 5.0, Disp = "5.0 Hz"},
                    new LowerBw() {Value = 3.0, Disp = "3.0 Hz"},
                    new LowerBw() {Value = 2.5, Disp = "2.5 Hz"},
                    new LowerBw() {Value = 2.0, Disp = "2.0 Hz"},
                    new LowerBw() {Value = 1.5, Disp = "1.5 Hz"},
                    new LowerBw() {Value = 1.0, Disp = "1.0 Hz"},
                    new LowerBw() {Value = 0.75, Disp = "0.75 Hz"},
                    new LowerBw() {Value = 0.50, Disp = "0.50 Hz"},
                    new LowerBw() {Value = 0.30, Disp = "0.30 Hz"},
                    new LowerBw() {Value = 0.25, Disp = "0.25 Hz"},
                    new LowerBw() {Value = 0.10, Disp = "0.10 Hz"},
                });
            }
        }
        #endregion lower bandwidth

        #region upper bandwidth
        private double _upperBw;
        public double UpperBw
        {
            get { return _upperBw; }
            set
            {
                if (Math.Abs(value - _upperBw) > 0.001)
                {
                    _upperBw = value;
                    _configuration.SetUpperBandwidth(value);
                    NotifyPropertyChange("UpperBW");
                }
            }
        }

        private ObservableCollection<UpperBw> _upperBwList;
        public ObservableCollection<UpperBw> UpperBwList
        {
            get
            {
                return _upperBwList ?? (_upperBwList = new ObservableCollection<UpperBw>()
                {
                    new UpperBw() {Value = 20000, Disp = "20 kHz"},
                    new UpperBw() {Value = 15000, Disp = "15 kHz"},
                    new UpperBw() {Value = 10000, Disp = "10 kHz"},
                    new UpperBw() {Value = 7500, Disp = "7.5 kHz"},
                    new UpperBw() {Value = 5000, Disp = "5.0 kHz"},
                    new UpperBw() {Value = 3000, Disp = "3.0 kHz"},
                    new UpperBw() {Value = 2500, Disp = "2.5 kHz"},
                    new UpperBw() {Value = 2000, Disp = "2.0 kHz"},
                    new UpperBw() {Value = 1500, Disp = "1.5 kHz"},
                    new UpperBw() {Value = 1000, Disp = "1.0 kHz"},
                    new UpperBw() {Value = 750, Disp = "750 Hz"},
                    new UpperBw() {Value = 500, Disp = "500 Hz"},
                    new UpperBw() {Value = 300, Disp = "300 Hz"},
                    new UpperBw() {Value = 250, Disp = "250 Hz"},
                    new UpperBw() {Value = 200, Disp = "200 Hz"},
                    new UpperBw() {Value = 150, Disp = "150 Hz"},
                    new UpperBw() {Value = 100, Disp = "100 Hz"},
                    new UpperBw() {Value = 75, Disp = "75 Hz"},
                    new UpperBw() {Value = 50, Disp = "50 Hz"},
                    new UpperBw() {Value = 30, Disp = "30 Hz"},
                    new UpperBw() {Value = 25, Disp = "25 Hz"},
                    new UpperBw() {Value = 20, Disp = "20 Hz"},
                    new UpperBw() {Value = 15, Disp = "15 Hz"},
                    new UpperBw() {Value = 10, Disp = "10 Hz"},
                });
            }
        }
        #endregion upper bandwidth

        #region DSP Cut-off

        private ObservableCollection<DspCutoffFreq> _dspCutoffList;
        public ObservableCollection<DspCutoffFreq> DSPCutoffList
        {
            get
            {
                return _dspCutoffList ?? (_dspCutoffList = new ObservableCollection<DspCutoffFreq>()
                {
                    new DspCutoffFreq() {Value = 0, Disp = "differentiator"},
                    new DspCutoffFreq() {Value = 1, Disp = (0.1103*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 2, Disp = (0.04579*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 3, Disp = (0.02125*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 4, Disp = (0.01027*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 5, Disp = (0.005053*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 6, Disp = (0.002506*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 7, Disp = (0.001248*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 8, Disp = (0.0006229*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 9, Disp = (0.0003112*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 10, Disp = (0.0001555*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 11, Disp = (0.00007773*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 12, Disp = (0.00003886*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 13, Disp = (0.00001943*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 14, Disp = (0.000009714*_samplingRate).ToString("F4") + "Hz"},
                    new DspCutoffFreq() {Value = 15, Disp = (0.000004857*_samplingRate).ToString("F4") + "Hz"}
                });
            }
            set
            {
                if (_dspCutoffList == value) return;
                _dspCutoffList = value;
                NotifyPropertyChange("DspCutoffList");
                NotifyPropertyChange("Configuration");
            }
        }
        #endregion DSP Cut-off

        #region ADC Output Format

        private ObservableCollection<ADCOutputFormat> _adcOutputFormatList;
        public ObservableCollection<ADCOutputFormat> ADCOutputFormatList
        {
            get
            {
                return _adcOutputFormatList ??
                       (_adcOutputFormatList = new ObservableCollection<ADCOutputFormat>()
                       {
                           new ADCOutputFormat() {Value = 0, Disp = "Unsigned"},
                           // Disabled as FPGA won't work with 2's compliment
                           //new ADCOutputFormat() {Value = 1, Disp = "2's compliment"},
                       });
            }
        }
        #endregion ADC Output Format

        private bool _calibrationEn;
        public bool CalibrationEn
        {
            get { return _calibrationEn; }
            set
            {
                if (value == _calibrationEn) return;
                _calibrationEn = value;
                NotifyPropertyChange("Calibration_EN");
            }
        }

        public RHD2132(int samplingrate, double lowerBw, double upperBw)
        {
            //int i;
            //var sampleRate = AmplifierSampleRate.SampleRate15000Hz; // Rhythm FPGA boots up with 30.0 kS/s/channel sampling rate
            _samplingRate = samplingrate;
            _lowerBw = lowerBw;
            _upperBw = upperBw;
            _configuration = new Rhd2000Registers(_samplingRate, _lowerBw, _upperBw);
            _calibrationEn = true;
            //numDataStreams = 0;

            //for (i = 0; i < AmplifierSampleRate.MAX_NUM_DATA_STREAMS; ++i) {
            //    dataStreamEnabled[i] = 0;
            //}

            //cableDelay.resize(4, -1);
        }

        //public int open();
        //bool uploadFpgaBitfile(string filename);
        //void initialize();

        //enum AmplifierSampleRate
        //{
        //    SampleRate1000Hz,
        //    SampleRate1250Hz,
        //    SampleRate1500Hz,
        //    SampleRate2000Hz,
        //    SampleRate2500Hz,
        //    SampleRate3000Hz,
        //    SampleRate3333Hz,
        //    SampleRate4000Hz,
        //    SampleRate5000Hz,
        //    SampleRate6250Hz,
        //    SampleRate8000Hz,
        //    SampleRate10000Hz,
        //    SampleRate12500Hz,
        //    SampleRate15000Hz,
        //    SampleRate20000Hz,
        //    SampleRate25000Hz,
        //    SampleRate30000Hz
        //};

        //bool setSampleRate(AmplifierSampleRate newSampleRate);
        //double getSampleRate();
        //AmplifierSampleRate getSampleRateEnum();

        enum AuxCmdSlot
        {
            AuxCmd1,
            AuxCmd2,
            AuxCmd3
        };

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange(string p)
        {
            //throw new NotImplementedException();
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(p));
        } 

        //void uploadCommandList(ref List<int> commandList, AuxCmdSlot auxCommandSlot, int bank);

        //void printCommandList(ref List<int> commandList)
        //{
        //    uint i = 0;
        //    int channel, reg, data;

        //    Console.WriteLine();

        //    foreach (var cmd in commandList)
        //    {
        //        if (cmd < 0 || cmd > 0xffff)
        //        {
        //            Console.WriteLine("  command[" + i + "] = INVALID COMMAND: " + cmd.ToString());
        //        }
        //        else if ((cmd & 0xc000) == 0x0000)
        //        {
        //            channel = (cmd & 0x3f00) >> 8;
        //            Console.WriteLine("  command[" + i + "] = CONVERT(" + channel + ")");
        //        }
        //        else if ((cmd & 0xc000) == 0xc000)
        //        {
        //            reg = (cmd & 0x3f00) >> 8;
        //            Console.WriteLine("  command[" + i + "] = READ(" + reg + ")");
        //        }
        //        else if ((cmd & 0xc000) == 0x8000)
        //        {
        //            reg = (cmd & 0x3f00) >> 8;
        //            data = (cmd & 0x00ff);
        //            Console.WriteLine("  command[" + i + "] = WRITE({0:X2})", reg);
        //        }
        //        else if (cmd == 0x5500)
        //        {
        //            Console.WriteLine("  command[" + i + "] = CALIBRATE");
        //        }
        //        else if (cmd == 0x6a00)
        //        {
        //            Console.WriteLine("  command[" + i + "] = CLEAR");
        //        }
        //        else
        //        {
        //            Console.WriteLine("  command[" + i + "] = INVALID COMMAND: {0:X4})", cmd);
        //        }
        //        i++;
        //    }
        //    Console.WriteLine();
        //}
    }
}
