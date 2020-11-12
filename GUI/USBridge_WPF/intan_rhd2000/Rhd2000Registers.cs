using System;
using System.Collections.Generic;
using System.Linq;

namespace INTAN_RHD2000

{
	/// <summary>
    /// This class creates and manages a data structure representing the internal RAM registers on
    /// a RHD2000 chip, and generates command lists to configure the chip and perform other functions.
    /// Changing the value of variables within an instance of this class does not directly affect a
    /// RHD2000 chip connected to the FPGA; rather, a command list must be generated from this class
    /// and then downloaded to the FPGA board using Rhd2000EvalBoard::uploadCommandList.
    /// </summary>
    public class Rhd2000Registers
	{

        double _sampleRate;

        public bool NotchEN { get; set; }

	    // RHD2000 Register 0 variables
        public int ADCReferenceBw { get; set; }
        public bool AmpFastSettle { get; set; }
        public int AmpVrefEnable{ get; set; }
        public int ADCComparatorBias{ get; set; }
        public int ADCComparatorSelect{ get; set; }

	    // RHD2000 Register 1 variables
        public bool VddSenseEnable{ get; set; }
        public int ADCBufferBias{ get; set; }

	    // RHD2000 Register 2 variables
        public int MuxBias{ get; set; }

	    // RHD2000 Register 3 variables
        public int MuxLoad{ get; set; }
        public bool TempS1 { get; set; }
        public bool TempS2 { get; set; }
        public bool TempEn{ get; set; }
        public bool DigOutHiZ{ get; set; }
        public int DigOut{ get; set; }

	    // RHD2000 Register 4 variables
        public bool WeakMiso { get; set; }
        public int TwosComp { get; set; }
        public bool AbsMode { get; set; }
        public bool DSPEn { get; set; }
        public int DSPCutoffFreq{ get; set; }


	    // RHD2000 Register 5 variables
        public int ZcheckDacPower{ get; set; }
        public int ZcheckLoad{ get; set; }
        public int ZcheckScale{ get; set; }
        public int ZcheckConnAll{ get; set; }
        public int ZcheckSelPol{ get; set; }
        public int ZcheckEn{ get; set; }

	    // RHD2000 Register 6 variables
	    //int zcheckDac;     // handle Zcheck DAC waveform elsewhere

	    // RHD2000 Register 7 variables
        public int ZcheckSelect{ get; set; }

	    // RHD2000 Register 8-13 variables
        public int OffChipRh1{ get; set; }
        public int OffChipRh2{ get; set; }
        public int OffChipRl{ get; set; }
        public bool ADCAux1En{ get; set; }
        public bool ADCAux2En { get; set; }
        public bool ADCAux3En { get; set; }
        public int Rh1DAC1{ get; set; }
        public int Rh1DAC2{ get; set; }
        public int Rh2DAC1{ get; set; }
        public int Rh2DAC2{ get; set; }
        public int RlDAC1{ get; set; }
        public int RlDAC2{ get; set; }
        public int RlDAC3{ get; set; }

	    // RHD2000 Register 14-17 variables: Individual Amplifier Power
        // Scaled for RHD2164.
        //int[] aPwr = new int[8];
        public byte[] APwr { get; set; }
        

        public enum ZcheckCs {
		    ZcheckCs100fF,
		    ZcheckCs1pF,
		    ZcheckCs10pF
	    };

	    public enum ZcheckPolarity {
		    ZcheckPositiveInput,
		    ZcheckNegativeInput
	    };

        public enum Rhd2000CommandType {
		    Rhd2000CommandConvert,
		    Rhd2000CommandCalibrate,
		    Rhd2000CommandCalClear,
		    Rhd2000CommandRegWrite,
		    Rhd2000CommandRegRead
	    };

	    /// <summary>
	    /// Constructor.  Set RHD2000 register variables to default values. 
	    /// </summary>
	    /// <param name="sampleRate"></param>
	    /// <param name="lowerBw"></param>
	    /// <param name="upperBw"></param>
	    public Rhd2000Registers(double sampleRate, double lowerBw, double upperBw)
        {
            APwr = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            DefineSampleRate(sampleRate);

            // Set default values for all register settings
            ADCReferenceBw = 3;         // ADC reference generator bandwidth (0 [highest BW] - 3 [lowest BW]);
                                        // always set to 3
            SetFastSettle(false);       // amplifier fast settle (off = normal operation)
            AmpVrefEnable = 1;          // enable amplifier voltage references (0 = power down; 1 = enable);
                                        // 1 = normal operation
            ADCComparatorBias = 3;      // ADC comparator preamp bias current (0 [lowest] - 3 [highest], only
                                        // valid for comparator select = 2,3); always set to 3
            ADCComparatorSelect = 2;    // ADC comparator select; always set to 2

            VddSenseEnable = false;         // supply voltage sensor enable (0 = disable; 1 = enable)
            // adcBufferBias = 32;      // ADC reference buffer bias current (0 [highest current] - 63 [lowest current]);
                                        // This value should be set according to ADC sampling rate; set in setSampleRate()

            // muxBias = 40;            // ADC input MUX bias current (0 [highest current] - 63 [lowest current]);
                                        // This value should be set according to ADC sampling rate; set in setSampleRate()

            // muxLoad = 0;             // MUX capacitance load at ADC input (0 [min CL] - 7 [max CL]); LSB = 3 pF
                                        // Set in setSampleRate()

            TempS1 = false;                 // temperature sensor S1 (0-1); 0 = power saving mode when temperature sensor is
                                        // not in use
            TempS2 = false;                 // temperature sensor S2 (0-1); 0 = power saving mode when temperature sensor is
                                        // not in use
            TempEn = false;                 // temperature sensor enable (0 = disable; 1 = enable)
            SetDigOutHiZ();             // auxiliary digital output state

            WeakMiso = false;               // weak MISO (0 = MISO line is HiZ when CS is inactive; 1 = MISO line is weakly
                                        // driven when CS is inactive)
            TwosComp = 0;               // two's complement ADC results (0 = unsigned offset representation; 1 = signed
                                        // representation)
            AbsMode = false;                // absolute value mode (0 = normal output; 1 = output passed through abs(x) function)
            EnableDSP(true);            // DSP offset removal enable/disable
            SetDspCutoffFreq(1.0);      // DSP offset removal HPF cutoff freqeuncy

            ZcheckDacPower = 0;         // impedance testing DAC power-up (0 = power down; 1 = power up)
            ZcheckLoad = 0;             // impedance testing dummy load (0 = normal operation; 1 = insert 60 pF to ground)
            SetZcheckScale(ZcheckCs.ZcheckCs100fF);  // impedance testing scale factor (100 fF, 1.0 pF, or 10.0 pF)
            ZcheckConnAll = 0;          // impedance testing connect all (0 = normal operation; 1 = connect all electrodes together)
            SetZcheckPolarity(ZcheckPolarity.ZcheckPositiveInput); // impedance testing polarity select (RHD2216 only) (0 = test positive inputs;
                                        // 1 = test negative inputs)
            EnableZcheck(false);        // impedance testing enable/disable

            SetZcheckChannel(0);        // impedance testing amplifier select (0-63)

            OffChipRh1 = 0;             // bandwidth resistor RH1 on/off chip (0 = on chip; 1 = off chip)
            OffChipRh2 = 0;             // bandwidth resistor RH2 on/off chip (0 = on chip; 1 = off chip)
            OffChipRl = 0;              // bandwidth resistor RL on/off chip (0 = on chip; 1 = off chip)
            ADCAux1En = true;              // enable ADC aux1 input (when RH1 is on chip) (0 = disable; 1 = enable)
            ADCAux2En = true;              // enable ADC aux2 input (when RH2 is on chip) (0 = disable; 1 = enable)
            ADCAux3En = true;              // enable ADC aux3 input (when RL is on chip) (0 = disable; 1 = enable)

            SetUpperBandwidth(upperBw); // set upper bandwidth of amplifiers

            SetLowerBandwidth(lowerBw);     // set lower bandwidth of amplifiers
            PowerUpAllAmps();           // turn on all amplifiers
        }
        
        /// <summary>
        /// // Define RHD2000 per-channel sampling rate so that certain sampling-rate-dependent registers are set correctly
        /// </summary>
        /// <param name="newSampleRate"></param>
	    public void DefineSampleRate(double newSampleRate)
        {
#if SingleCh
            sampleRate = newSampleRate / 32;
#else
            _sampleRate = newSampleRate;
#endif

            MuxLoad = 0;

            if (_sampleRate < 3334.0) {
                MuxBias = 40;
                ADCBufferBias = 32;
            } else if (_sampleRate < 4001.0) {
                MuxBias = 40;
                ADCBufferBias = 16;
            } else if (_sampleRate < 5001.0) {
                MuxBias = 40;
                ADCBufferBias = 8;
            } else if (_sampleRate < 6251.0) {
                MuxBias = 32;
                ADCBufferBias = 8;
            } else if (_sampleRate < 8001.0) {
                MuxBias = 26;
                ADCBufferBias = 8;
            } else if (_sampleRate < 10001.0) {
                MuxBias = 18;
                ADCBufferBias = 4;
            } else if (_sampleRate < 12501.0) {
                MuxBias = 16;
                ADCBufferBias = 3;
            } else if (_sampleRate < 15001.0) {
                MuxBias = 7;
                ADCBufferBias = 3;
            } else {
                MuxBias = 4;
                ADCBufferBias = 2;
            }
        }

        /// <summary>
        /// // Enable or disable amplifier fast settle function; drive amplifiers to baseline if enabled.
        /// </summary>
        /// <param name="enabled"></param>
	    public void SetFastSettle(bool enabled)
        {
            AmpFastSettle = enabled;
        }

        /// <summary>
        /// Drive auxiliary digital output low
        /// </summary>
	    public void SetDigOutLow()
        {
            DigOut = 0;
            DigOutHiZ = false;
        }

        /// <summary>
        /// Drive auxiliary digital output high
        /// </summary>
	    public void SetDigOutHigh()
        {
            DigOut = 1;
            DigOutHiZ = false;
        }
        /// <summary>
        /// Set auxiliary digital output to high-impedance (HiZ) state
        /// </summary>
	    public void SetDigOutHiZ()
        {
            DigOut = 0;
            DigOutHiZ = true;
        }

        /// <summary>
        /// Enable or disable ADC auxiliary input 1
        /// </summary>
        /// <param name="enabled"></param>
	    public void EnableAux1(bool enabled)
        {
            ADCAux1En = enabled;
        }

        /// <summary>
        /// Enable or disable ADC auxiliary input 2
        /// </summary>
        /// <param name="enabled"></param>
	    public void EnableAux2(bool enabled)
        {
            ADCAux2En = enabled;
        }

        /// <summary>
        /// Enable or disable ADC auxiliary input 3
        /// </summary>
        /// <param name="enabled"></param>
	    public void EnableAux3(bool enabled)
        {
            ADCAux3En = enabled;
        }

        /// <summary>
        /// Enable or disable DSP offset removal filter
        /// </summary>
        /// <param name="enabled"></param>
	    public void EnableDSP(bool enabled)
        {
            DSPEn = enabled;
        }

        /// <summary>
        /// Set the DSP offset removal filter cutoff frequency as closely to the requested
        /// newDspCutoffFreq (in Hz) as possible; returns the actual cutoff frequency (in Hz).
        /// </summary>
        /// <param name="newDspCutoffFreq"></param>
        /// <returns></returns>
	    public double SetDspCutoffFreq(double newDspCutoffFreq)
        {
            int n;
            double [] fCutoff = new double[16], logFCutoff = new double[16];

            fCutoff[0] = 0.0;   // We will not be using fCutoff[0], but we initialize it to be safe

            var logNewDspCutoffFreq = Math.Log10(newDspCutoffFreq);

            // Generate table of all possible DSP cutoff frequencies
            for (n = 1; n < 16; ++n) {
                var x = Math.Pow(2.0, n);
                fCutoff[n] = _sampleRate * Math.Log(x / (x - 1.0)) / (2 * Math.PI);
                logFCutoff[n] = Math.Log10(fCutoff[n]);
                // cout << "  fCutoff[" << n << "] = " << fCutoff[n] << " Hz" << endl;
            }

            // Now find the closest value to the requested cutoff frequency (on a logarithmic scale)
            if (newDspCutoffFreq > fCutoff[1]) {
                DSPCutoffFreq = 1;
            } else if (newDspCutoffFreq < fCutoff[15]) {
                DSPCutoffFreq = 15;
            } else
            {
                var minLogDiff = 10000000.0;
                for (n = 1; n < 16; ++n) {
                    if (!(Math.Abs(logNewDspCutoffFreq - logFCutoff[n]) < minLogDiff)) continue;
                    minLogDiff = Math.Abs(logNewDspCutoffFreq - logFCutoff[n]);
                    DSPCutoffFreq = n;
                }
            }

            return fCutoff[DSPCutoffFreq];
        }

        /// <summary>
        /// Returns the current value of the DSP offset removal cutoff frequency (in Hz).
        /// </summary>
        /// <returns></returns>
	    public double GetDspCutoffFreq()
        {
            var x = Math.Pow(2.0, DSPCutoffFreq);
            return _sampleRate * Math.Log(x / (x - 1.0)) / (2*Math.PI);
        }

	    /// <summary>
        /// Enable or disable impedance checking mode
        /// </summary>
        /// <param name="enabled"></param>
	    public void EnableZcheck(bool enabled)
        {
            ZcheckEn = enabled ? 1: 0;
        }

        /// <summary>
        /// Power up or down impedance checking DAC
        /// </summary>
        /// <param name="enabled"></param>
	    public void SetZcheckDacPower(bool enabled)
        {
            ZcheckDacPower = enabled ? 1 : 0;
        }

        /// <summary>
        /// Select the series capacitor used to convert the voltage waveform generated by the on-chip
        /// DAC into an AC current waveform that stimulates a selected electrode for impedance testing
        /// (ZcheckCs100fF, ZcheckCs1pF, or Zcheck10pF).
        /// </summary>
        /// <param name="scale"></param>
	    public void SetZcheckScale(ZcheckCs scale)
        {
            switch (scale) {
                case ZcheckCs.ZcheckCs100fF:
                    ZcheckScale = 0x00;     // Cs = 0.1 pF
                    break;
                case ZcheckCs.ZcheckCs1pF:
                    ZcheckScale = 0x01;     // Cs = 1.0 pF
                    break;
                case ZcheckCs.ZcheckCs10pF:
                    ZcheckScale = 0x03;     // Cs = 10.0 pF
                    break;
            }
        }

        /// <summary>
        /// Select impedance testing of positive or negative amplifier inputs (RHD2216 only), based
        /// on the variable polarity (ZcheckPositiveInput or ZcheckNegativeInput)
        /// </summary>
        /// <param name="polarity"></param>
	    public void SetZcheckPolarity(ZcheckPolarity polarity)
        {
            switch (polarity) {
                case ZcheckPolarity.ZcheckPositiveInput:
                    ZcheckSelPol = 0;
                    break;
                case ZcheckPolarity.ZcheckNegativeInput:
                    ZcheckSelPol = 1;
                    break;
            }
        }

        /// <summary>
        /// Select the amplifier channel (0-63) for impedance testing.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
	    public int SetZcheckChannel(int channel)
        {
            if (channel < 0 || channel > 63) {
                return -1;
            } else {
                ZcheckSelect = channel;
                return ZcheckSelect;
            }
        }

        /// <summary>
        /// Power up or down selected amplifier on chip
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="powered"></param>
	    public void SetAmpPowered(int channel, bool powered)
        {
            if (channel >= 0 && channel <= 63) {
                if (powered)
                    APwr[channel/8] |= (byte)(1 << (channel % 8));
                else
                    APwr[channel/8] &= (byte)(~(1 << (channel % 8)));
            }
        }

        /// <summary>
        /// Power up all amplifiers on chip
        /// </summary>
	    public void PowerUpAllAmps()
        {
            for(var i=0; i<APwr.Length;i++)
            {
                APwr[i] = 0xFF;
            }
        }

        /// <summary>
        /// Power down all amplifiers on chip
        /// </summary>
	    public void PowerDownAllAmps()
        {
            for(var i=0; i<APwr.Length;i++)
            {
                APwr[i] = 0;
            }
        }

        /// <summary>
        /// Returns the value of a selected RAM register (0-17) on the RHD2000 chip, based
        /// on the current register variables in the class instance.
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
	    int GetRegisterValue(int reg)
        {
            int regout;
            const int zcheckDac = 128;  // midrange

            switch (reg) {
                case 0:
                    regout = (ADCReferenceBw << 6) + (Convert.ToInt16(AmpFastSettle) << 5) + (AmpVrefEnable << 4) +
                            (ADCComparatorBias << 2) + ADCComparatorSelect;
                    break;
                case 1:
                    regout = (Convert.ToInt16(VddSenseEnable) << 6) + ADCBufferBias;
                    break;
                case 2:
                    regout = MuxBias;
                    break;
                case 3:
                    regout = (MuxLoad << 5) + (Convert.ToInt16(TempS2) << 4) + (Convert.ToInt16(TempS1) << 3) + (Convert.ToInt16(TempEn) << 2) + (Convert.ToInt16(DigOutHiZ) << 1) + DigOut;
                    break;
                case 4:
                    regout = (Convert.ToInt16(WeakMiso) << 7) + (TwosComp << 6) + (Convert.ToInt16(AbsMode) << 5) + (Convert.ToInt16(DSPEn) << 4) + DSPCutoffFreq;
                    break;
                case 5:
                    regout = (ZcheckDacPower << 6) + (ZcheckLoad << 5) + (ZcheckScale << 3) + (ZcheckConnAll << 2) + (ZcheckSelPol << 1) + ZcheckEn;
                    break;
                case 6:
                    regout = zcheckDac;
                    break;
                case 7:
                    regout = ZcheckSelect;
                    break;
                case 8:
                    regout = (OffChipRh1 << 7) + Rh1DAC1;
                    break;
                case 9:
                    regout = (Convert.ToInt16(ADCAux1En) << 7) + Rh1DAC2;
                    break;
                case 10:
                    regout = (OffChipRh2 << 7) + Rh2DAC1;
                    break;
                case 11:
                    regout = (Convert.ToInt16(ADCAux2En) << 7) + Rh2DAC2;
                    break;
                case 12:
                    regout = (OffChipRl << 7) + RlDAC1;
                    break;
                case 13:
                    regout = (Convert.ToInt16(ADCAux3En) << 7) + (RlDAC3 << 6) + RlDAC2;
                    break;
                case 14:
                    regout = APwr[0];
                    break;
                case 15:
                    regout = APwr[1];
                    break;
                case 16:
                    regout = APwr[2];
                    break;
                case 17:
                    regout = APwr[3];
                    break;
                case 18:
                    regout = APwr[4];
                    break;
                case 19:
                    regout = APwr[5];
                    break;
                case 20:
                    regout = APwr[6];
                    break;
                case 21:
                    regout = APwr[7];
                    break;
                default:
                    regout = -1;
                    break;
            }
            return regout;
        }

        /// <summary>
        /// Sets the on-chip RH1 and RH2 DAC values appropriately to set a particular amplifier
        /// upper bandwidth (in Hz).  Returns an estimate of the actual upper bandwidth achieved.
        /// </summary>
        /// <param name="upperBandwidth"></param>
        /// <returns></returns>
	    public double SetUpperBandwidth(double upperBandwidth)
        {
            const double rh1Base = 2200.0;
            const double rh1Dac1Unit = 600.0;
            const double rh1Dac2Unit = 29400.0;
            const int rh1Dac1Steps = 63;
            const int rh1Dac2Steps = 31;

            const double rh2Base = 8700.0;
            const double rh2Dac1Unit = 763.0;
            const double rh2Dac2Unit = 38400.0;
            const int rh2Dac1Steps = 63;
            const int rh2Dac2Steps = 31;

            int i;

            // Upper bandwidths higher than 30 kHz don't work well with the RHD2000 amplifiers
            if (upperBandwidth > 30000.0) {
                upperBandwidth = 30000.0;
            }

            var rH1Target = Rh1FromUpperBandwidth(upperBandwidth);

            Rh1DAC1 = 0;
            Rh1DAC2 = 0;
            var rH1Actual = rh1Base;

            for (i = 0; i < rh1Dac2Steps; ++i) {
                if (!(rH1Actual < rH1Target - (rh1Dac2Unit - rh1Dac1Unit/2))) continue;
                rH1Actual += rh1Dac2Unit;
                ++Rh1DAC2;
            }

            for (i = 0; i < rh1Dac1Steps; ++i) {
                if (!(rH1Actual < rH1Target - (rh1Dac1Unit/2))) continue;
                rH1Actual += rh1Dac1Unit;
                ++Rh1DAC1;
            }

            var rH2Target = Rh2FromUpperBandwidth(upperBandwidth);

            Rh2DAC1 = 0;
            Rh2DAC2 = 0;
            var rH2Actual = rh2Base;

            for (i = 0; i < rh2Dac2Steps; ++i) {
                if (rH2Actual < rH2Target - (rh2Dac2Unit - rh2Dac1Unit / 2)) {
                    rH2Actual += rh2Dac2Unit;
                    ++Rh2DAC2;
                }
            }

            for (i = 0; i < rh2Dac1Steps; ++i) {
                if (rH2Actual < rH2Target - (rh2Dac1Unit / 2)) {
                    rH2Actual += rh2Dac1Unit;
                    ++Rh2DAC1;
                }
            }

            var actualUpperBandwidth1 = UpperBandwidthFromRh1(rH1Actual);
            var actualUpperBandwidth2 = UpperBandwidthFromRh2(rH2Actual);

            // Upper bandwidth estimates calculated from actual RH1 value and acutal RH2 value
            // should be very close; we will take their geometric mean to get a single
            // number.
            var actualUpperBandwidth = Math.Sqrt(actualUpperBandwidth1 * actualUpperBandwidth2);

            /*
            cout << endl;
            cout << "Rhd2000Registers::setUpperBandwidth" << endl;
            cout << fixed << setprecision(1);

            cout << "RH1 DAC2 = " << rH1Dac2 << ", DAC1 = " << rH1Dac1 << endl;
            cout << "RH1 target: " << rH1Target << " Ohms" << endl;
            cout << "RH1 actual: " << rH1Actual << " Ohms" << endl;

            cout << "RH2 DAC2 = " << rH2Dac2 << ", DAC1 = " << rH2Dac1 << endl;
            cout << "RH2 target: " << rH2Target << " Ohms" << endl;
            cout << "RH2 actual: " << rH2Actual << " Ohms" << endl;

            cout << "Upper bandwidth target: " << upperBandwidth << " Hz" << endl;
            cout << "Upper bandwidth actual: " << actualUpperBandwidth << " Hz" << endl;

            cout << endl;
            cout << setprecision(6);
            cout.unsetf(ios::floatfield);
            */

            return actualUpperBandwidth;
        }

        /// <summary>
        /// Sets the on-chip RL DAC values appropriately to set a particular amplifier
        /// lower bandwidth (in Hz).  Returns an estimate of the actual lower bandwidth achieved.
        /// </summary>
        /// <param name="lowerBandwidth"></param>
        /// <returns></returns>
	    public double SetLowerBandwidth(double lowerBandwidth)
        {
            const double rlBase = 3500.0;
            const double rlDac1Unit = 175.0;
            const double rlDac2Unit = 12700.0;
            const double rlDac3Unit = 3000000.0;
            const int rlDac1Steps = 127;
            const int rlDac2Steps = 63;

            int i;

            // Lower bandwidths higher than 1.5 kHz don't work well with the RHD2000 amplifiers
            if (lowerBandwidth > 1500.0) {
                lowerBandwidth = 1500.0;
            }

            var rLTarget = RlFromLowerBandwidth(lowerBandwidth);

            RlDAC1 = 0;
            RlDAC2 = 0;
            RlDAC3 = 0;
            var rLActual = rlBase;

            if (lowerBandwidth < 0.15) {
                rLActual += rlDac3Unit;
                ++RlDAC3;
            }

            for (i = 0; i < rlDac2Steps; ++i) {
                if (rLActual < rLTarget - (rlDac2Unit - rlDac1Unit / 2)) {
                    rLActual += rlDac2Unit;
                    ++RlDAC2;
                }
            }

            for (i = 0; i < rlDac1Steps; ++i) {
                if (rLActual < rLTarget - (rlDac1Unit / 2)) {
                    rLActual += rlDac1Unit;
                    ++RlDAC1;
                }
            }

            var actualLowerBandwidth = lowerBandwidthFromRL(rLActual);

            /*
            cout << endl;
            cout << fixed << setprecision(1);
            cout << "Rhd2000Registers::setLowerBandwidth" << endl;

            cout << "RL DAC3 = " << rLDac3 << ", DAC2 = " << rLDac2 << ", DAC1 = " << rLDac1 << endl;
            cout << "RL target: " << rLTarget << " Ohms" << endl;
            cout << "RL actual: " << rLActual << " Ohms" << endl;

            cout << setprecision(3);

            cout << "Lower bandwidth target: " << lowerBandwidth << " Hz" << endl;
            cout << "Lower bandwidth actual: " << actualLowerBandwidth << " Hz" << endl;

            cout << endl;
            cout << setprecision(6);
            cout.unsetf(ios::floatfield);
            */
            return actualLowerBandwidth;
        }

        /// <summary>
        /// Create a list of 60 commands to program most RAM registers on a RHD2000 chip, read those values
        /// back to confirm programming, read ROM registers, and (if calibrate == true) run ADC calibration.
        /// Returns the length of the command list.
        /// </summary>
        /// <param name="commandList"></param>
        /// <param name="calibrate"></param>
        /// <returns></returns>
	    public int CreateCommandListRegisterConfig(ref List<int> commandList, bool calibrate)
        {
            commandList.Clear();   // if command list already exists, erase it and start a new one

            // Start with a few dummy commands in case chip is still powering up or has entered calibrate.
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));

            // Program RAM registers
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 0, GetRegisterValue(0)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 1, GetRegisterValue(1)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 2, GetRegisterValue(2)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 4, GetRegisterValue(4)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 5, GetRegisterValue(5)));
            // Don't program Register 6 (Impedance Check DAC) here; create DAC waveform in another command stream
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 7, GetRegisterValue(7)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 8, GetRegisterValue(8)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 9, GetRegisterValue(9)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 10, GetRegisterValue(10)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 11, GetRegisterValue(11)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 12, GetRegisterValue(12)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 13, GetRegisterValue(13)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 14, GetRegisterValue(14)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 15, GetRegisterValue(15)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 16, GetRegisterValue(16)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 17, GetRegisterValue(17)));

            // Read ROM registers
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 62));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 61));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 60));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 59));

            // Read chip name from ROM
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 48));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 49));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 50));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 51));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 52));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 53));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 54));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 55));

            // Read Intan name from ROM
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 40));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 41));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 42));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 43));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 44));

            // Read back RAM registers to confirm programming
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 0));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 1));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 2));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 3));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 4));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 5));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 6));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 7));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 8));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 9));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 10));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 11));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 12));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 13));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 14));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 15));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 16));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 17));

            // Optionally, run ADC calibration (should only be run once after board is plugged in)
            if (calibrate)
            {
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandCalibrate));
                // Must send 9 dummy commands for calibration commands.
                for (int i=0;i<9;i++)
                    commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));
            }
            //// Added in Version 1.2:
            //// Program amplifier 31-63 power up/down registers in case a RHD2164 is connected
            //// Note: We don't read these registers back, since they are only 'visible' on MISO B.
            //commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 18, getRegisterValue(18)));
            //commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 19, getRegisterValue(19)));
            //commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 20, getRegisterValue(20)));
            //commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 21, getRegisterValue(21)));

            // End with a dummy command
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));

            return commandList.Count();
        }

        /// <summary>
        /// Create a list of 60 commands to sample auxiliary ADC inputs, temperature sensor, and supply
        /// voltage sensor.  One temperature reading (one sample of ResultA and one sample of ResultB)
        /// is taken during this 60-command sequence.  One supply voltage sample is taken.  Auxiliary
        /// ADC inputs are continuously sampled at 1/4 the amplifier sampling rate.
        ///
        /// Since this command list consists of writing to Register 3, it also sets the state of the
        /// auxiliary digital output.  If the digital output value needs to be changed dynamically,
        /// then variations of this command list need to be generated for each state and programmed into
        /// different RAM banks, and the appropriate command list selected at the right time.
        ///
        ///sss Returns the length of the command list.
        /// </summary>
        /// <param name="commandList"></param>
        /// <returns></returns>
	    public int CreateCommandListTempSensor(ref List<int> commandList)
        {
            int i;

            commandList.Clear();    // if command list already exists, erase it and start a new one

            TempEn = true;

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            TempS1 = TempEn;
            TempS2 = false;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            TempS1 = TempEn;
            TempS2 = TempEn;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 49));     // sample Temperature Sensor
            
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            TempS1 = false;
            TempS2 = TempEn;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 49));     // sample Temperature Sensor

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            TempS1 = false;
            TempS2 = false;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 48));     // sample Supply Voltage Sensor

            for (i = 0; i < 8; ++i)
            {
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 32));     // sample AuxIn1
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 33));     // sample AuxIn2
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandConvert, 34));     // sample AuxIn3
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegRead, 63));      // dummy command
            }

            return commandList.Count();
        }

        /// <summary>
        /// Create a list of 60 commands to update Register 3 (controlling the auxiliary digital ouput
        /// pin) every sampling period.
        ///
        /// Since this command list consists of writing to Register 3, it also sets the state of the
        /// on-chip temperature sensor.  The temperature sensor settings are therefore changed throughout
        /// this command list to coordinate with the 60-command list generated by createCommandListTempSensor().
        ///
        /// Returns the length of the command list.
        /// </summary>
        /// <param name="commandList"></param>
        /// <returns></returns>
	    public int CreateCommandListUpdateDigOut(ref List<int> commandList)
        {
            int i;

            commandList.Clear();    // if command list already exists, erase it and start a new one

            TempEn = true;

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            TempS1 = TempEn;
            TempS2 = false;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            TempS1 = TempEn;
            TempS2 = TempEn;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            TempS1 = false;
            TempS2 = TempEn;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            TempS1 = false;
            TempS2 = false;
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));

            for (i = 0; i < 8; ++i)
            {
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
                commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 3, GetRegisterValue(3)));
            }

            return commandList.Count();
        }

        /// <summary>
        /// Create a list of up to 1024 commands to generate a sine wave of particular frequency (in Hz) and
        /// amplitude (in DAC steps, 0-128) using the on-chip impedance testing voltage DAC.  If frequency is set to zero,
        /// a DC baseline waveform is created.
        /// Returns the length of the command list.       
        /// </summary>
        /// <param name="commandList"></param>
        /// <param name="frequency"></param>
        /// <param name="amplitude"></param>
        /// <returns></returns>
	    public int CreateCommandListZcheckDac(ref List<int> commandList, double frequency, double amplitude)
        {
            int i;

            commandList.Clear();    // if command list already exists, erase it and start a new one

            if (amplitude < 0.0 || amplitude > 128.0)
            {
                Console.Error.WriteLine("Error in Rhd2000Registers::createCommandListZcheckDac: Amplitude out of range.");
                return -1;
            }
            if (frequency < 0.0)
            {
                Console.Error.WriteLine("Error in Rhd2000Registers::createCommandListZcheckDac: Negative frequency not allowed.");
                return -1;
            }
            else if (frequency > _sampleRate / 4.0)
            {
                Console.Error.WriteLine("Error in Rhd2000Registers::createCommandListZcheckDac: " +
                        "Frequency too high relative to sampling rate.");
                return -1;
            }
            if (Math.Abs(frequency) < 0.01)
            {
                for (i = 0; i < MaxCommandLength; ++i)
                {
                    commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 6, 128));
                }
            }
            else
            {
                var period = (int)Math.Floor(_sampleRate / frequency + 0.5);
                if (period > MaxCommandLength)
                {
                    Console.Error.WriteLine("Error in Rhd2000Registers::createCommandListZcheckDac: Frequency too low.");
                    return -1;
                }
                else
                {
                    var t = 0.0;
                    for (i = 0; i < period; ++i)
                    {
                        var value = (int)Math.Floor(amplitude * Math.Sin(2 * Math.PI * frequency * t) + 128.0 + 0.5);
                        if (value < 0)
                        {
                            value = 0;
                        }
                        else if (value > 255)
                        {
                            value = 255;
                        }
                        commandList.Add(CreateRhd2000Command(Rhd2000CommandType.Rhd2000CommandRegWrite, 6, value));
                        t += 1.0 / _sampleRate;
                    }
                }
            }

            return commandList.Count();
        }


        /// <summary>
        /// Return a 16-bit MOSI command (CALIBRATE or CLEAR)
        /// </summary>
        /// <param name="commandType"></param>
        /// <returns></returns>
	    public int CreateRhd2000Command(Rhd2000CommandType commandType)
        {
            switch (commandType) {
                case Rhd2000CommandType.Rhd2000CommandCalibrate:
                    return 0x5500;   // 0101010100000000
                case Rhd2000CommandType.Rhd2000CommandCalClear:
                    return 0x6a00;   // 0110101000000000
                default:
                    Console.Error.WriteLine ("Error in Rhd2000Registers::CreateRhd2000Command: " +
                        "Only 'Calibrate' or 'Clear Calibration' commands take zero arguments.");
                    return -1;
            }
        }

        /// <summary>
        /// Return a 16-bit MOSI command (CONVERT or READ)
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="arg1"></param>
        /// <returns></returns>
	    public int CreateRhd2000Command(Rhd2000CommandType commandType, int arg1)
        {
            switch (commandType) {
                case Rhd2000CommandType.Rhd2000CommandConvert:
                    if (arg1 < 0 || arg1 > 63) {
                        Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                                "Channel number out of range.");
                        return -1;
                    }
                    return 0x0000 + (arg1 << 8);  // 00cccccc0000000h; if the command is 'Convert',
                                                  // arg1 is the channel number
                case Rhd2000CommandType.Rhd2000CommandRegRead:
                    if (arg1 < 0 || arg1 > 63) {
                        Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                                "Register address out of range.");
                        return -1;
                    }
                    return 0xc000 + (arg1 << 8);  // 11rrrrrr00000000; if the command is 'Register Read',
                                                  // arg1 is the register address
                default:
                    Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                            "Only 'Convert' and 'Register Read' commands take one argument.");
                    return -1;
            }
        }

        /// <summary>
        /// Return a 16-bit MOSI command (WRITE)
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
	    public int CreateRhd2000Command(Rhd2000CommandType commandType, int arg1, int arg2)
        {
            switch (commandType) {
                case Rhd2000CommandType.Rhd2000CommandRegWrite:
                    if (arg1 < 0 || arg1 > 63) {
                        Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                                "Register address out of range.");
                        return -1;
                    }
                    if (arg2 < 0 || arg2 > 255) {
                        Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                                "Register data out of range.");
                        return -1;
                    }
                    return 0x8000 + (arg1 << 8) + arg2; // 10rrrrrrdddddddd; if the command is 'Register Write',
                                                        // arg1 is the register address and arg2 is the data
                default:
                    Console.Error.WriteLine("Error in Rhd2000Registers::CreateRhd2000Command: " +
                            "Only 'Register Write' commands take two arguments.");
                    return -1;
            }
        }

        /// <summary>
        /// Returns the value of the RH1 resistor (in ohms) corresponding to a particular upper
        /// bandwidth value (in Hz).
        /// </summary>
        /// <param name="upperBandwidth"></param>
        /// <returns></returns>
        static double Rh1FromUpperBandwidth(double upperBandwidth)
        {
            var log10F = Math.Log10(upperBandwidth);

            return 0.9730 * Math.Pow(10.0, (8.0968 - 1.1892 * log10F + 0.04767 * log10F * log10F));
        }

        /// <summary>
        /// Returns the value of the RH2 resistor (in ohms) corresponding to a particular upper
        /// bandwidth value (in Hz).
        /// </summary>
        /// <param name="upperBandwidth"></param>
        /// <returns></returns>
        static double Rh2FromUpperBandwidth(double upperBandwidth)
        {
            var log10F = Math.Log10(upperBandwidth);

            return 1.0191 * Math.Pow(10.0, (8.1009 - 1.0821 * log10F + 0.03383 * log10F * log10F));
        }

        /// <summary>
        /// Returns the value of the RL resistor (in ohms) corresponding to a particular lower
        /// bandwidth value (in Hz).
        /// </summary>
        /// <param name="lowerBandwidth"></param>
        /// <returns></returns>
        static double RlFromLowerBandwidth(double lowerBandwidth)
        {
            var log10F = Math.Log10(lowerBandwidth);

            if (lowerBandwidth < 4.0) {
                return 1.0061 * Math.Pow(10.0, (4.9391 - 1.2088 * log10F + 0.5698 * log10F * log10F +
                                           0.1442 * log10F * log10F * log10F));
            } else {
                return 1.0061 * Math.Pow(10.0, (4.7351 - 0.5916 * log10F + 0.08482 * log10F * log10F));
            }
        }

        /// <summary>
        /// Returns the amplifier upper bandwidth (in Hz) corresponding to a particular value
        /// of the resistor RH1 (in ohms).
        /// </summary>
        /// <param name="rH1"></param>
        /// <returns></returns>
        static double UpperBandwidthFromRh1(double rH1)
        {
            const double a = 0.04767;
            const double b = -1.1892;
            var c = 8.0968 - Math.Log10(rH1/0.9730);

            return Math.Pow(10.0, ((-b - Math.Sqrt(b * b - 4 * a * c))/(2 * a)));
        }

        /// <summary>
        /// Returns the amplifier upper bandwidth (in Hz) corresponding to a particular value
        /// of the resistor RH2 (in ohms).
        /// </summary>
        /// <param name="rH2"></param>
        /// <returns></returns>
        static double UpperBandwidthFromRh2(double rH2)
        {
            const double a = 0.03383;
            const double b = -1.0821;
            var c = 8.1009 - Math.Log10(rH2/1.0191);

            return Math.Pow(10.0, ((-b - Math.Sqrt(b * b - 4 * a * c))/(2 * a))); 
        }

        /// <summary>
        /// Returns the amplifier lower bandwidth (in Hz) corresponding to a particular value
        /// of the resistor RL (in ohms).
        /// </summary>
        /// <param name="rL"></param>
        /// <returns></returns>
	    double lowerBandwidthFromRL(double rL)
        {
            double a, b, c;

            // Quadratic fit below is invalid for values of RL less than 5.1 kOhm
            if (rL < 5100.0) {
                rL = 5100.0;
            }

            if (rL < 30000.0) {
                a = 0.08482;
                b = -0.5916;
                c = 4.7351 - Math.Log10(rL/1.0061);
            } else {
                a = 0.3303;
                b = -1.2100;
                c = 4.9873 - Math.Log10(rL/1.0061);
            }

            return Math.Pow(10.0, ((-b - Math.Sqrt(b * b - 4 * a * c))/(2 * a))); 
        }

	    const int MaxCommandLength = 1024; // size of on-FPGA auxiliary command RAM banks

	}

    public class SamplingRate
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }
    public class LowerBw
    {
        public double Value { get; set; }
        public string Disp { get; set; }
    }

    public class UpperBw
    {
        public double Value { get; set; }
        public string Disp { get; set; }
    }

    public class DspCutoffFreq
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }



    public class ADCOutputFormat
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }
}
