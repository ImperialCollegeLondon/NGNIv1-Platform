using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTAN_RHD2000
{
    public class RHD2132_Igloo:RHD2132
    {
        public int LSBMapBit
        {
            get { return _lsbMapBit; }
            set
            {
                if (_lsbMapBit == value) return;
                _lsbMapBit = value;
                FullRangeValue = (0.195 * Math.Pow(2, LSBMapBit) * FullRangeBit);
                NotifyPropertyChange("LSBMapBit");
                NotifyPropertyChange("FullRangeValue");
            }
        }

        public double FullRangeValue { get; private set; }
        public double FullRangeBit = 512.0; // Used for normalisation. Use double type


        public bool Connected { get; set; }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                NotifyPropertyChange("Enabled");
            }
        }

        #region 16 to 9-bit mapping

        private ObservableCollection<LSBMapBit16To9> _lsbMappingList;
        private int _lsbMapBit;
        private bool _enabled;

        public ObservableCollection<LSBMapBit16To9> LsbMappingList
        {
            get
            {
                return _lsbMappingList ?? (_lsbMappingList = new ObservableCollection<LSBMapBit16To9>()
                {
                    new LSBMapBit16To9() {Value = 0, Disp = "0.195 uV"},
                    new LSBMapBit16To9() {Value = 1, Disp = "0.39 uV"},
                    new LSBMapBit16To9() {Value = 2, Disp = "0.78 uV"},
                    new LSBMapBit16To9() {Value = 3, Disp = "1.56 uV"},
                    new LSBMapBit16To9() {Value = 4, Disp = "3.12 uV"},
                    new LSBMapBit16To9() {Value = 5, Disp = "6.24 uV"},
                    new LSBMapBit16To9() {Value = 6, Disp = "12.48 uV"},
                    new LSBMapBit16To9() {Value = 7, Disp = "24.96 uV"}
                });
            }
        }
        #endregion 16 to 9-bit mapping

        public RHD2132_Igloo(int samplingrate, double lowerBw, double upperBw, int defaultLSB)
            : base(samplingrate, lowerBw, upperBw)
        {
            LSBMapBit = defaultLSB;
        }
    }

    public class LSBMapBit16To9
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }
}
