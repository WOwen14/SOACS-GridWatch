using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SOACS.GridWatch.Models;
using SOACS.GridWatch.Services;

namespace SOACS.GridWatch
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        readonly DispatcherTimer _clock = new DispatcherTimer();
        readonly DispatcherTimer _scheduler = new DispatcherTimer();
        readonly MonitorService _monitor = new MonitorService();
        readonly DiscoveryService _discovery = new DiscoveryService();
        readonly ConfigurationService _config = new ConfigurationService();
        readonly SemaphoreSlim _checkThrottle = new SemaphoreSlim(10);
        bool _running;
        bool _schedulerBusy;
        string _clockText;
        TargetNode _selectedTarget;
        string _appDataRoot;
        string _configFolder;
        string _docsFolder;
        string _logsFolder;
        string _exportsFolder;
        string _profilesFolder;
        string _logFile;

        public ObservableCollection<TargetNode> Targets { get; private set; }
        public ObservableCollection<string> EventLog { get; private set; }
        public ObservableCollection<string> SelectedTargetLog { get; private set; }
        public Array MonitorTypes { get { return Enum.GetValues(typeof(MonitorType)); } }
        public Array ProfileTypes { get { return Enum.GetValues(typeof(ProfileType)); } }
        public string ClockText { get { return _clockText; } set { _clockText=value; OnChanged(); } }
        public TargetNode SelectedTarget { get { return _selectedTarget; } set { _selectedTarget=value; OnChanged(); RefreshSelectedTargetLog(); } }
        public int OnlineCount { get { return Targets.Count(x=>x.Enabled && x.State==NodeState.Online); } }
        public int OfflineCount { get { return Targets.Count(x=>x.Enabled && x.State==NodeState.Offline); } }
        public int WarningCount { get { return Targets.Count(x=>x.Enabled && x.State==NodeState.Warning); } }
        public int EnabledCount { get { return Targets.Count(x=>x.Enabled); } }
        public string EngineHealthText { get { return "Monitoring Engine: " + (_running ? "RUNNING" : "STOPPED") + " | Targets: " + Targets.Count + " | Active Checks: " + Targets.Count(x => x.IsCheckInProgress) + " | Scheduler: 1 sec"; } }
        public bool CanStart { get { return !_running; } }
        public bool CanStop { get { return _running; } }
        public Brush StartButtonBrush { get { return _running ? new SolidColorBrush(Color.FromRgb(32,45,52)) : new SolidColorBrush(Color.FromRgb(18,70,42)); } }
        public Brush StopButtonBrush { get { return _running ? new SolidColorBrush(Color.FromRgb(110,24,42)) : new SolidColorBrush(Color.FromRgb(32,45,52)); } }
        public Brush StartButtonTextBrush { get { return _running ? new SolidColorBrush(Color.FromRgb(143,239,255)) : new SolidColorBrush(Color.FromRgb(51,255,133)); } }
        public Brush StopButtonTextBrush { get { return _running ? new SolidColorBrush(Color.FromRgb(255,120,145)) : new SolidColorBrush(Color.FromRgb(143,239,255)); } }
        public Brush EngineStatusBrush { get { return _running ? new SolidColorBrush(Color.FromRgb(51,255,133)) : new SolidColorBrush(Color.FromRgb(255,51,102)); } }

        public MainWindow()
        {
            InitializeComponent();
            Targets = new ObservableCollection<TargetNode>();
            EventLog = new ObservableCollection<string>();
            SelectedTargetLog = new ObservableCollection<string>();
            DataContext = this;
            EnsureAppDataStructure();
            AddDefaults();
            SelectedTarget = Targets.FirstOrDefault();
            _clock.Interval = TimeSpan.FromSeconds(1); _clock.Tick += delegate { ClockText = DateTime.Now.ToString("HH:mm:ss"); }; _clock.Start();
            _scheduler.Interval = TimeSpan.FromSeconds(1); _scheduler.Tick += Scheduler_Tick; _scheduler.Start();
            OpenMaximizedToWorkArea();
            Log("SYSTEM", "SOACS GridWatch v1.0 RC2 initialized");
        }

        void OpenMaximizedToWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            WindowState = WindowState.Normal;
            Left = wa.Left;
            Top = wa.Top;
            Width = wa.Width;
            Height = wa.Height;
            WindowState = WindowState.Maximized;
        }

        void AddDefaults()
        {
            Targets.Add(new TargetNode{ Name="TRAX CoT", Address="127.0.0.1", Monitor=MonitorType.TCP, Port=4800, Profile=ProfileType.Normal, Enabled=true });
            Targets.Add(new TargetNode{ Name="Gateway Ping", Address="192.168.1.1", Monitor=MonitorType.ICMP, Profile=ProfileType.Normal, Enabled=true });
            Targets.Add(new TargetNode{ Name="UDP Service", Address="127.0.0.1", Monitor=MonitorType.UDP, Port=6969, Profile=ProfileType.Normal, Enabled=false });
        }

        async void Scheduler_Tick(object sender, EventArgs e)
        {
            if (!_running || _schedulerBusy) return;

            _schedulerBusy = true;
            try
            {
                DateTime now = DateTime.Now;
                foreach (var n in Targets)
                {
                    if (n.Enabled && !n.IsMonitorRunning)
                    {
                        n.IsMonitorRunning = true;
                        n.CycleStart = now;
                        n.NextCheck = now;
                    }
                    if (!n.Enabled && n.IsMonitorRunning) n.IsMonitorRunning = false;
                    n.UpdateNextProgress();
                }

                var due = Targets.Where(x => x.Enabled && !x.IsCheckInProgress && now >= x.NextCheck).ToList();
                if (due.Count > 0)
                    await Task.WhenAll(due.Select(x => CheckNodeAsync(x, false)));

                RefreshCounts();
            }
            finally
            {
                _schedulerBusy = false;
            }
        }

        async Task CheckNodeAsync(TargetNode node, bool manual)
        {
            if (node == null || !node.Enabled || node.IsCheckInProgress) return;

            await _checkThrottle.WaitAsync();
            node.IsCheckInProgress = true;

            try
            {
                node.State = NodeState.Checking;
                node.NextProgress = 100;

                var result = await _monitor.CheckAsync(node, 1000);
                node.Response = result.Response;
                node.RxPackets += result.RxIncrement;
                node.RecordCheck(result.Online);
                node.State = result.Online ? NodeState.Online : NodeState.Offline;
                if (result.Online) node.LastSeen = DateTime.Now;
                node.StartCycle(DateTime.Now);
                Log(node.Name, (result.Online ? "ONLINE - " : "OFFLINE - ") + result.Response + (manual ? " [MANUAL]" : ""));
                RefreshCounts();
            }
            finally
            {
                node.IsCheckInProgress = false;
                _checkThrottle.Release();
            }
        }

        void Log(string source, string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + source + "  " + msg;
            EventLog.Insert(0, line);
            while (EventLog.Count > 300) EventLog.RemoveAt(EventLog.Count - 1);
            try
            {
                if (string.IsNullOrEmpty(_logFile)) EnsureAppDataStructure();
                File.AppendAllText(_logFile, DateTime.Now.ToString("yyyy-MM-dd ") + line + Environment.NewLine);
            }
            catch
            {
                // Log file write failure should not interrupt monitoring.
            }
            RefreshSelectedTargetLog();
        }

        void RefreshSelectedTargetLog()
        {
            if (SelectedTargetLog == null) return;
            SelectedTargetLog.Clear();
            if (SelectedTarget == null || string.IsNullOrWhiteSpace(SelectedTarget.Name)) return;
            string token = "  " + SelectedTarget.Name + "  ";
            foreach (var item in EventLog.Where(x => x.Contains(token)).Take(100))
                SelectedTargetLog.Add(item);
        }

        void RefreshCounts()
        {
            OnChanged("OnlineCount"); OnChanged("OfflineCount"); OnChanged("WarningCount"); OnChanged("EnabledCount");
            OnChanged("EngineHealthText"); OnChanged("EngineStatusBrush");
            OnChanged("CanStart"); OnChanged("CanStop"); OnChanged("StartButtonBrush"); OnChanged("StopButtonBrush");
            OnChanged("StartButtonTextBrush"); OnChanged("StopButtonTextBrush");
        }

        void Start_Click(object sender, RoutedEventArgs e){ _running=true; foreach(var n in Targets){ n.IsMonitorRunning=n.Enabled; n.CycleStart=DateTime.Now; n.NextCheck=DateTime.Now; n.UpdateNextProgress(); } Log("SYSTEM","Monitoring started"); }
        void Stop_Click(object sender, RoutedEventArgs e){ _running=false; foreach(var n in Targets){ n.IsMonitorRunning=false; n.IsCheckInProgress=false; n.State = n.Enabled ? n.State : NodeState.Disabled; n.UpdateNextProgress(); } Log("SYSTEM","Monitoring stopped"); }
        async void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            var enabled = Targets.Where(x=>x.Enabled).ToList();
            await Task.WhenAll(enabled.Select(x => CheckNodeAsync(x, true)));
        }
        async void CheckOne_Click(object sender, RoutedEventArgs e){ await CheckNodeAsync(SelectedTarget, true); }
        void AddTarget_Click(object sender, RoutedEventArgs e){ var n=new TargetNode(); Targets.Add(n); SelectedTarget=n; RefreshCounts(); }
        void RemoveTarget_Click(object sender, RoutedEventArgs e){ if(SelectedTarget!=null){ Targets.Remove(SelectedTarget); SelectedTarget=Targets.FirstOrDefault(); RefreshCounts(); } }
        void DuplicateTarget_Click(object sender, RoutedEventArgs e){ if(SelectedTarget==null) return; var s=SelectedTarget; var n=new TargetNode{Enabled=s.Enabled,Name=s.Name+" Copy",Address=s.Address,Monitor=s.Monitor,Port=s.Port,Profile=s.Profile}; Targets.Add(n); SelectedTarget=n; RefreshCounts(); }
        async void Discover_Click(object sender, RoutedEventArgs e)
        {
            Log("DISCOVERY","Scanning local network");
            var found = await _discovery.DiscoverLocalAsync();
            int added=0;
            foreach(var d in found)
            {
                if(Targets.Any(t=>t.Address==d.Address)) continue;
                Targets.Add(new TargetNode{Enabled=true,Name=string.IsNullOrEmpty(d.Hostname)?"Discovered "+d.Address:d.Hostname,Address=d.Address,Monitor=MonitorType.ICMP,Profile=ProfileType.Normal}); added++;
            }
            Log("DISCOVERY", "Found " + found.Count + " reachable, added " + added);
            RefreshCounts();
        }
        void Save_Click(object sender, RoutedEventArgs e)
        {
            EnsureAppDataStructure();
            var dlg = new SaveFileDialog
            {
                Title = "Save GridWatch Configuration",
                Filter = "GridWatch Config (*.xml)|*.xml",
                DefaultExt = ".xml",
                AddExtension = true,
                InitialDirectory = _configFolder,
                FileName = "GridWatchConfig.xml"
            };
            if (dlg.ShowDialog() == true)
            {
                _config.Save(dlg.FileName, Targets);
                Log("SYSTEM", "Config saved: " + dlg.FileName);
            }
        }

        void Load_Click(object sender, RoutedEventArgs e)
        {
            EnsureAppDataStructure();
            var dlg = new OpenFileDialog
            {
                Title = "Load GridWatch Configuration",
                Filter = "GridWatch Config (*.xml)|*.xml",
                InitialDirectory = _configFolder
            };
            if (dlg.ShowDialog() == true)
            {
                Targets.Clear();
                foreach (var n in _config.Load(dlg.FileName)) Targets.Add(n);
                SelectedTarget = Targets.FirstOrDefault();
                Log("SYSTEM", "Config loaded: " + dlg.FileName);
                RefreshCounts();
            }
        }
        void ClearLog_Click(object sender, RoutedEventArgs e){ EventLog.Clear(); RefreshSelectedTargetLog(); }
        void SummaryMode_Click(object sender, RoutedEventArgs e)
        {
            var summary = new Window();
            summary.Title = "SOACS GridWatch";
            summary.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/AppIcon.ico"));
            summary.Width = 420;
            summary.Height = 330;
            summary.MinWidth = 360;
            summary.MinHeight = 240;
            summary.WindowStartupLocation = WindowStartupLocation.Manual;
            summary.Left = SystemParameters.WorkArea.Right - summary.Width - 20;
            summary.Top = SystemParameters.WorkArea.Top + 40;
            summary.Background = new SolidColorBrush(Color.FromRgb(5, 11, 16));
            summary.Foreground = new SolidColorBrush(Color.FromRgb(216, 251, 255));
            summary.Topmost = true;

            var root = new Grid();
            root.Margin = new Thickness(10);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var logo = new Image();
            logo.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/HeaderLogo.png"));
            logo.Width = 40;
            logo.Height = 40;
            logo.Stretch = System.Windows.Media.Stretch.Uniform;
            Grid.SetColumn(logo, 0);
            header.Children.Add(logo);
            var title = new TextBlock();
            title.Text = "SOACS GRIDWATCH";
            title.FontSize = 18;
            title.FontWeight = FontWeights.Bold;
            title.FontFamily = new FontFamily("Consolas");
            title.Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255));
            title.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(title, 1);
            header.Children.Add(title);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var counts = new TextBlock();
            counts.FontSize = 13;
            counts.FontFamily = new FontFamily("Consolas");
            counts.Margin = new Thickness(0, 8, 0, 8);
            Grid.SetRow(counts, 1);
            root.Children.Add(counts);

            var rows = new StackPanel();
            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Content = rows;
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            var returnButton = new Button();
            returnButton.Content = "RETURN TO FULL MODE";
            returnButton.Height = 30;
            returnButton.Margin = new Thickness(0, 10, 0, 0);
            returnButton.Click += delegate
            {
                summary.Close();
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };
            Grid.SetRow(returnButton, 3);
            root.Children.Add(returnButton);

            Action render = delegate
            {
                counts.Text = "ENGINE: " + (_running ? "RUNNING" : "STOPPED") + "\nONLINE: " + OnlineCount + "        OFFLINE: " + OfflineCount;
                rows.Children.Clear();
                foreach (var n in Targets.Where(x => x.Enabled))
                {
                    var border = new Border();
                    border.CornerRadius = new CornerRadius(6);
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));
                    border.BorderThickness = new Thickness(1);
                    border.Margin = new Thickness(0, 0, 0, 4);
                    border.Padding = new Thickness(5);
                    border.Background = new SolidColorBrush(Color.FromRgb(7, 23, 34));

                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });

                    var dot = new Ellipse();
                    dot.Width = 12;
                    dot.Height = 12;
                    dot.Stroke = new SolidColorBrush(Color.FromRgb(216, 251, 255));
                    dot.Fill = n.State == NodeState.Online ? new SolidColorBrush(Color.FromRgb(51, 255, 133)) :
                               n.State == NodeState.Warning ? new SolidColorBrush(Color.FromRgb(255, 211, 77)) :
                               n.State == NodeState.Checking ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) :
                               new SolidColorBrush(Color.FromRgb(255, 51, 102));
                    dot.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(dot, 0);
                    row.Children.Add(dot);

                    var text = new TextBlock();
                    text.Text = n.Name;
                    text.FontSize = 12;
                    text.FontFamily = new FontFamily("Consolas");
                    text.TextTrimming = TextTrimming.CharacterEllipsis;
                    text.Foreground = new SolidColorBrush(Color.FromRgb(216, 251, 255));
                    text.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(text, 1);
                    row.Children.Add(text);

                    var ip = new TextBlock();
                    ip.Text = n.Address;
                    ip.FontSize = 12;
                    ip.FontFamily = new FontFamily("Consolas");
                    ip.TextTrimming = TextTrimming.CharacterEllipsis;
                    ip.Foreground = new SolidColorBrush(Color.FromRgb(143, 239, 255));
                    ip.VerticalAlignment = VerticalAlignment.Center;
                    ip.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetColumn(ip, 2);
                    row.Children.Add(ip);

                    border.Child = row;
                    rows.Children.Add(border);
                }
            };

            var refresh = new DispatcherTimer();
            refresh.Interval = TimeSpan.FromSeconds(1);
            refresh.Tick += delegate { render(); };
            summary.Closed += delegate { refresh.Stop(); };
            render();
            refresh.Start();

            summary.Content = root;
            Hide();
            summary.Show();
        }

        void OperatorGuide_Click(object sender, RoutedEventArgs e)
        {
            OpenDocumentationFile("Operator_Guide.pdf");
        }

        void Help_Click(object sender, RoutedEventArgs e)
        {
            OpenDocumentationFile("Help.pdf");
        }

        void WhatsNew_Click(object sender, RoutedEventArgs e)
        {
            OpenDocumentationFile("Whats_New.pdf");
        }

        void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            EnsureAppDataStructure();
            Process.Start(new ProcessStartInfo("explorer.exe", _configFolder) { UseShellExecute = true });
            Log("SYSTEM", "Opened config folder: " + _configFolder);
        }

        void EnsureAppDataStructure()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _appDataRoot = System.IO.Path.Combine(local, "SOACS", "GridWatch");
            _configFolder = System.IO.Path.Combine(_appDataRoot, "Config");
            _docsFolder = System.IO.Path.Combine(_appDataRoot, "Docs");
            _logsFolder = System.IO.Path.Combine(_appDataRoot, "Logs");
            _exportsFolder = System.IO.Path.Combine(_appDataRoot, "Exports");
            _profilesFolder = System.IO.Path.Combine(_appDataRoot, "Profiles");
            _logFile = System.IO.Path.Combine(_logsFolder, "GridWatch.log");

            Directory.CreateDirectory(_appDataRoot);
            Directory.CreateDirectory(_configFolder);
            Directory.CreateDirectory(_docsFolder);
            Directory.CreateDirectory(_logsFolder);
            Directory.CreateDirectory(System.IO.Path.Combine(_logsFolder, "Archive"));
            Directory.CreateDirectory(_exportsFolder);
            Directory.CreateDirectory(_profilesFolder);
            CopyDocumentationToAppData();
        }

        void CopyDocumentationToAppData()
        {
            CopyDocumentationFile("Operator_Guide.pdf");
            CopyDocumentationFile("Help.pdf");
            CopyDocumentationFile("Whats_New.pdf");
        }

        void CopyDocumentationFile(string fileName)
        {
            string destination = System.IO.Path.Combine(_docsFolder, fileName);
            string appDoc = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", fileName);
            string sourceDoc = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Docs", fileName));
            string source = File.Exists(appDoc) ? appDoc : sourceDoc;
            try
            {
                if (File.Exists(source)) File.Copy(source, destination, true);
            }
            catch
            {
                // Documentation copy failure should not prevent monitoring startup.
            }
        }

        void OpenDocumentationFile(string fileName)
        {
            EnsureAppDataStructure();
            string selected = System.IO.Path.Combine(_docsFolder, fileName);

            if (!File.Exists(selected))
            {
                CopyDocumentationFile(fileName);
            }

            if (!File.Exists(selected))
            {
                MessageBox.Show("Documentation file not found:\n" + selected, "SOACS GridWatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log("SYSTEM", "Documentation file not found: " + selected);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(selected) { UseShellExecute = true });
                Log("SYSTEM", "Opened documentation: " + selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open documentation:\n" + ex.Message, "SOACS GridWatch", MessageBoxButton.OK, MessageBoxImage.Error);
                Log("SYSTEM", "Unable to open documentation: " + fileName + " - " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged; void OnChanged([CallerMemberName] string n=null){ var h=PropertyChanged; if(h!=null) h(this,new PropertyChangedEventArgs(n)); }
    }
}
