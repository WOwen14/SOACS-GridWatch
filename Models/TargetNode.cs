using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SOACS.GridWatch.Models
{
    public class TargetNode : INotifyPropertyChanged
    {
        bool _enabled;
        string _name;
        string _address;
        MonitorType _monitor;
        int _port;
        ProfileType _profile;
        NodeState _state;
        string _response;
        DateTime? _lastSeen;
        DateTime _nextCheck;
        DateTime _cycleStart;
        int _rx;
        double _nextProgress;
        bool _isMonitorRunning;
        bool _isCheckInProgress;
        int _successChecks;
        int _totalChecks;

        public bool Enabled { get { return _enabled; } set { _enabled = value; OnChanged(); OnChanged("StateText"); OnChanged("NextText"); OnChanged("ScannerActive"); OnChanged("StatusToolTip"); } }
        public string Name { get { return _name; } set { _name = value; OnChanged(); } }
        public string Address { get { return _address; } set { _address = value; OnChanged(); } }
        public MonitorType Monitor { get { return _monitor; } set { _monitor = value; if (value == MonitorType.ICMP) Port = 0; OnChanged(); OnChanged("MonitorText"); OnChanged("PortText"); OnChanged("PortEntryText"); OnChanged("IsPortEditable"); OnChanged("StatusToolTip"); } }
        public int Port { get { return _port; } set { _port = value; OnChanged(); OnChanged("PortText"); OnChanged("PortEntryText"); OnChanged("MonitorText"); OnChanged("StatusToolTip"); } }
        public ProfileType Profile { get { return _profile; } set { _profile = value; OnChanged(); OnChanged("PollSeconds"); OnChanged("ProfileText"); OnChanged("StatusToolTip"); } }
        public NodeState State { get { return _state; } set { _state = value; OnChanged(); OnChanged("StateText"); OnChanged("StatusBrush"); OnChanged("NextText"); OnChanged("StatusToolTip"); } }
        public string Response { get { return _response; } set { _response = value; OnChanged(); OnChanged("StatusToolTip"); } }
        public DateTime? LastSeen { get { return _lastSeen; } set { _lastSeen = value; OnChanged(); OnChanged("LastSeenText"); OnChanged("StatusToolTip"); } }
        public DateTime NextCheck { get { return _nextCheck; } set { _nextCheck = value; OnChanged(); OnChanged("NextText"); UpdateNextProgress(); } }
        public DateTime CycleStart { get { return _cycleStart; } set { _cycleStart = value; OnChanged(); UpdateNextProgress(); } }
        public double NextProgress { get { return _nextProgress; } set { _nextProgress = value; OnChanged(); } }
        public bool IsMonitorRunning { get { return _isMonitorRunning; } set { _isMonitorRunning = value; OnChanged(); OnChanged("NextText"); OnChanged("ScannerActive"); if (!value) NextProgress = 0; } }
        public bool IsCheckInProgress { get { return _isCheckInProgress; } set { _isCheckInProgress = value; OnChanged(); OnChanged("NextText"); } }
        public bool ScannerActive { get { return Enabled && IsMonitorRunning; } }
        public int RxPackets { get { return _rx; } set { _rx = value; OnChanged(); } }
        public int SuccessChecks { get { return _successChecks; } set { _successChecks = value; OnChanged(); OnChanged("History"); OnChanged("UptimePercent"); } }
        public int TotalChecks { get { return _totalChecks; } set { _totalChecks = value; OnChanged(); OnChanged("History"); OnChanged("UptimePercent"); } }
        public double UptimePercent { get { return TotalChecks <= 0 ? 0 : (SuccessChecks * 100.0) / TotalChecks; } }
        public string History { get { return TotalChecks <= 0 ? "—" : UptimePercent.ToString("0.0") + "%"; } }
        public int PollSeconds { get { if (Profile == ProfileType.Critical) return 2; if (Profile == ProfileType.High) return 5; if (Profile == ProfileType.Low) return 30; return 10; } }
        public string StateText { get { return !Enabled ? "DISABLED" : State.ToString().ToUpperInvariant(); } }
        public string LastSeenText { get { return LastSeen.HasValue ? LastSeen.Value.ToString("HH:mm:ss") : "Never"; } }
        public string NextText { get { if (!Enabled) return "OFF"; if (!IsMonitorRunning) return "STOPPED"; return State == NodeState.Checking ? "CHECKING" : "NEXT"; } }
        public string PortText { get { return Monitor == MonitorType.ICMP ? "—" : (Port > 0 ? Port.ToString() : ""); } }
        public bool IsPortEditable { get { return Monitor != MonitorType.ICMP; } }
        public string PortEntryText
        {
            get { return Monitor == MonitorType.ICMP ? "—" : (Port > 0 ? Port.ToString() : ""); }
            set
            {
                if (Monitor == MonitorType.ICMP) return;
                int n;
                if (int.TryParse(value, out n)) Port = n;
                else if (string.IsNullOrWhiteSpace(value)) Port = 0;
            }
        }
        public string MonitorText { get { return Monitor == MonitorType.ICMP ? "PING" : Monitor + ":" + PortText; } }
        public string ProfileText { get { return Profile.ToString(); } }
        public Brush StatusBrush { get { if (!Enabled || State == NodeState.Disabled) return Brushes.SlateGray; if (State == NodeState.Online) return Brushes.Lime; if (State == NodeState.Warning) return Brushes.Gold; if (State == NodeState.Offline) return Brushes.DeepPink; return Brushes.DodgerBlue; } }
        public string StatusToolTip
        {
            get
            {
                string status = !Enabled ? "Target Disabled" : (State == NodeState.Online ? "Target Online" : State == NodeState.Offline ? "Target Offline" : State == NodeState.Checking ? "Checking Target" : "Target Status Unknown");
                string detail = status + Environment.NewLine +
                                "Name: " + (string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name) + Environment.NewLine +
                                "Address: " + (string.IsNullOrWhiteSpace(Address) ? "(blank)" : Address) + Environment.NewLine +
                                "Monitor: " + MonitorText + Environment.NewLine +
                                "Profile: " + ProfileText + " (" + PollSeconds + " sec)" + Environment.NewLine +
                                "Response: " + (string.IsNullOrWhiteSpace(Response) ? "Not checked" : Response) + Environment.NewLine +
                                "Last Seen: " + LastSeenText + Environment.NewLine +
                                "Uptime: " + History;
                return detail;
            }
        }

        public void RecordCheck(bool online)
        {
            TotalChecks++;
            if (online) SuccessChecks++;
        }

        public void StartCycle(DateTime now)
        {
            CycleStart = now;
            NextCheck = now.AddSeconds(PollSeconds);
            UpdateNextProgress();
        }

        public void UpdateNextProgress()
        {
            if (!Enabled || !IsMonitorRunning)
            {
                NextProgress = 0;
                return;
            }
            double total = (NextCheck - CycleStart).TotalMilliseconds;
            if (total <= 0) { NextProgress = 100; return; }
            double elapsed = (DateTime.Now - CycleStart).TotalMilliseconds;
            if (elapsed < 0) elapsed = 0;
            if (elapsed > total) elapsed = total;
            NextProgress = (elapsed / total) * 100.0;
        }

        public TargetNode()
        {
            Enabled = true;
            Name = "New Target";
            Address = "127.0.0.1";
            Monitor = MonitorType.ICMP;
            Profile = ProfileType.Normal;
            State = NodeState.Unknown;
            Response = "Not checked";
            CycleStart = DateTime.Now;
            NextCheck = DateTime.Now;
            IsMonitorRunning = false;
            IsCheckInProgress = false;
            NextProgress = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnChanged([CallerMemberName] string n = null) { var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(n)); }
    }
}
