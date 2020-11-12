using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace USBridge_WPF
{
    public class NeuralChannel
    {
        #region Properties
        public ConcurrentQueue<NeuralSignal> NeuralSignals { get; set; }
        public ConcurrentQueue<NeuralSignal> SpikeScopeFIFO { get; set; }
        public ConcurrentQueue<SpikeData> SpikeEvents { get; set; }
        public List<ushort> CacheStream { get; set; }
        public List<ushort> SpikeCacheStream { get; set; }
        public Configuration ChannelParameter { get; set; }
        public ulong LastEventTime  { get; set; }
        public bool IsDisplaying { get; set; }
        public bool IsSpikeDetecting { get; set; }
        #endregion
        /// <summary>
        /// Initializes a new instance of the <see cref="NeuralChannel"/> class.
        /// rawSignal and spikeSignal are lists used to store relevant data respectively
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sequenceId"></param>
        public NeuralChannel(string name, int sequenceId)
        {
            NeuralSignals = new ConcurrentQueue<NeuralSignal>();
            SpikeScopeFIFO = new ConcurrentQueue<NeuralSignal>();
            SpikeEvents = new ConcurrentQueue<SpikeData>();
            CacheStream = new List<ushort>();
            SpikeCacheStream = new List<ushort>();
            ChannelParameter = new Configuration(name, sequenceId, 0, new[]
                                {new Configuration.Template(){ MatchingTh=0, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                                new Configuration.Template(){ MatchingTh=0, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                                new Configuration.Template(){ MatchingTh=0, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}},
                                new Configuration.Template(){ MatchingTh=0, Samples= new short[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}}}
                                );
            LastEventTime = 0;
            IsDisplaying = false;
            IsSpikeDetecting = false;
        }

        /// <summary>
        /// class to store spike data
        /// </summary>
        public class SpikeData
        {
            public ulong Time { get; set; }
            public ushort TemplateId { get; set; }

            public SpikeData(ulong time, ushort templateId)
            {
                Time = time;
                TemplateId = templateId;
            }
        }


        /// <summary>
        /// class to store neural signal
        /// </summary>
        public class NeuralSignal
        {
            public ulong Time { get; set; }

            public ushort Amplitude { get; set; }

            public NeuralSignal(ulong time, ushort amplitude)
            {
                Time = time;
                Amplitude = amplitude;
            }
        }

        public class RawData
        {
            public bool IsSpikeEvent { get; set; }
            public uint Addr { get; set; }
            public uint Channel { get; set; }
            public ulong Time { get; set; }
            public ushort OriginalData { get; set; }

            public RawData(bool isSpikeEvent, uint addr, uint channel, ulong time, ushort originalData)
            {
                IsSpikeEvent = isSpikeEvent;
                Addr = addr;
                Channel = channel;
                Time = time;
                OriginalData = originalData;
            }
        }

        /// <summary>
        /// class to store channel configuration
        /// </summary>
        public class Configuration : INotifyPropertyChanged
        {
            private string _name;
            private int _sequenceId;
            private short _detectTh;
            //private UInt16 _DetectSp;
            private bool _active;
            private Template[] _templates;

            #region Properties

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        NotifyPropertyChange("Name");
                    }
                }
            }

            public int SequenceId
            {
                get { return _sequenceId; }
                set
                {
                    if (_sequenceId == value) return;
                    _sequenceId = value;
                    NotifyPropertyChange("sequenceID");
                }
            }

            public short DetectTh
            {
                get { return _detectTh; }
                set
                {
                    if (_detectTh == value) return;
                    _detectTh = value;
                    NotifyPropertyChange("DetectTh");
                }
            }

            //public UInt16 DetectSp
            //{
            //    get { return this._DetectSp; }
            //    set
            //    {
            //        if (this._DetectSp != value)
            //        {
            //            this._DetectSp = value;
            //            this.NotifyPropertyChange("DetectSp");
            //        }
            //    }
            //}

            public bool Active
            {
                get { return this._active; }
                set
                {
                    if (this._active != value)
                    {
                        this._active = value;
                        this.NotifyPropertyChange("Active");
                    }
                }
            }

            public Template[] Templates
            {
                get { return _templates; }
                set
                {
                    if (_templates == value) return;
                    _templates = value;
                    NotifyPropertyChange("Templates");
                }
            }
            #endregion

            public class Template
            {
                private short _matchingTh;
                private short[] _samples;
                //private byte _ID;

                public Template()
                {
                    Samples = new short[16];
                }

                //public byte ID
                //{
                //    get { return this._ID; }
                //    set { this._ID = value; }
                //}

                public short MatchingTh
                {
                    get { return _matchingTh; }
                    set
                    {
                        if (_matchingTh != value)
                        {
                            _matchingTh = value;
                            NotifyPropertyChange("MatchingTh");
                        }
                    }
                }

                public short[] Samples
                {
                    get { return _samples; }
                    set
                    {
                        if (_samples != value)
                        {
                            _samples = value;
                            NotifyPropertyChange("Samples");
                        }
                    }
                }

                //public Template(Int16 matchinTh, Int16[] samples)
                //{
                //    //this._ID = ID;
                //    this._matchingTh = matchinTh;
                //    this._samples = samples;
                //}

                #region Notify Event
                public event PropertyChangedEventHandler PropertyChanged;
                public void NotifyPropertyChange(string p)
                {
                    //throw new NotImplementedException();
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs(p));
                }
                #endregion
            }

            public Configuration(string name, int sequenceId, short detectTh, Template[] templates)
            {
                _name = name;
                _sequenceId = sequenceId;
                //this._Active = Active;
                _detectTh = detectTh;
                //this._DetectSp = DetectSp;
                _templates = templates;
            }

            #region Notify Event
            public event PropertyChangedEventHandler PropertyChanged;
            public void NotifyPropertyChange(string p)
            {
                //throw new NotImplementedException();
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
            #endregion
        }
    }
}
