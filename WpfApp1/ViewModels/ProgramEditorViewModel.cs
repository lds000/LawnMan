// ProgramEditorViewModel with verbose auto-save logging
using BackyardBoss.Commands;
using BackyardBoss.Data;
using BackyardBoss.Dialogs;
using BackyardBoss.Models;
using BackyardBoss.Services;
using BackyardBoss.UserControls;
using BackyardBoss.ViewModels;
using BackyardBoss.Views;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WpfApp1;

namespace BackyardBoss.ViewModels
{
    public class PiRunInfo
    {
        [JsonPropertyName("set")]
        public string Set { get; set; }
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }
        [JsonPropertyName("duration_minutes")]
        public int? DurationMinutes { get; set; }
        [JsonPropertyName("phase")]
        public string Phase { get; set; }
        [JsonPropertyName("time_remaining_sec")]
        public int? TimeRemainingSec { get; set; }
        [JsonPropertyName("pulse_time_left_sec")]
        public int? PulseTimeLeftSec { get; set; }
        [JsonPropertyName("soak_remaining_sec")]
        public int? SoakRemainingSec { get; set; }
    }

    public class LastCompletedRunInfo
    {
        [JsonPropertyName("set")]
        public string Set { get; set; }
        [JsonPropertyName("end_time")]
        public string EndTime { get; set; }
        [JsonPropertyName("duration_minutes")]
        public int? DurationMinutes { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class UpcomingRunInfo
    {
        [JsonPropertyName("set")]
        public string Set { get; set; }
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }
        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        public string StartTimeDisplay =>
       DateTime.TryParse(StartTime, out var dt)
           ? dt.ToString("MMM dd, yyyy 'at' h:mm tt")
           : StartTime;

        public int SeasonallyAdjustedMinutes
        {
            get => (int)Math.Round(DurationMinutes * ProgramEditorViewModel.Current?.SeasonalAdjustment ?? 1.0);
        }
    }

    public class PiStatusResponse
    {
        [JsonPropertyName("system_status")]
        public string SystemStatus { get; set; }
        [JsonPropertyName("test_mode")]
        public bool TestMode { get; set; }
        [JsonPropertyName("test_mode_timestamp")]
        public double TestModeTimestamp { get; set; }
        [JsonPropertyName("led_colors")]
        public Dictionary<string, string> LedColors { get; set; }
        [JsonPropertyName("zones")]
        public List<ZoneStatus> Zones { get; set; }
        [JsonPropertyName("current_run")]
        public PiRunInfo CurrentRun { get; set; }
        [JsonPropertyName("next_run")]
        public PiRunInfo NextRun { get; set; }
        [JsonPropertyName("last_completed_run")]
        public LastCompletedRunInfo LastCompletedRun { get; set; }
        [JsonPropertyName("upcoming_runs")]
        public List<UpcomingRunInfo> UpcomingRuns { get; set; }
        [JsonPropertyName("today_is_watering_day")]
        public bool TodayIsWateringDay { get; set; }
        [JsonPropertyName("mist_status")]
        public BackyardBoss.Models.MistStatus MistStatus { get; set; } // <-- Add this property
    }

    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        #region Fields
        private SprinklerSet _selectedSet;
        private string _selectedStartTime;
        private bool _isDirty;
        private int _todayScheduleIndex = -1;
        private string _statusIconPath;
        private bool _isSet1On, _isSet2On, _isSet3On;
        private Brush _set1Color = Brushes.LightGray;
        private Brush _set2Color = Brushes.LightGray;
        private Brush _set3Color = Brushes.LightGray;
        private DateTime _lastManualTestModeChange = DateTime.MinValue;
        private bool _isTestMode;
        private bool _piReportedTestMode;
        private bool _suppressExport = false;
        private string _currentStation;
        private string _countdown;
        private static readonly object _saveLock = new();
        private int? _piScheduleIndex;
        private string _piLocalTime;
        private string _piTimezone;
        private string _selectedSection = "Overview";
        private string _systemStatus = "Unknown";
        private string _nextRunDisplay = "-";
        private string _lastRunDisplay = "-";
        private ObservableCollection<ZoneStatus> _zoneStatuses = new();
        private ObservableCollection<WateringLogEntry> _lastWeekHistory = new();
        private DispatcherTimer _debounceSaveTimer;
        private Dictionary<string, string> _ledColors = new();
        private PiRunInfo _currentRun;
        private PiRunInfo _nextRun;
        private LastCompletedRunInfo _lastCompletedRun;
        private ObservableCollection<UpcomingRunInfo> _upcomingRuns = new();
        private BackyardBoss.Models.MistStatus _mistStatus;
        private readonly DispatcherTimer _timeTimer;
        private ImageSource _mapImageSource;
        private ObservableCollection<SprinklerLineModel> _sharedSprinklerLines = new();
        private ObservableCollection<SprinklerSet> _visibleSets = new();
        private SprinklerLineModel? _selectedMapLine;
        private bool _isWindy;
        private ObservableCollection<SensorReading> _sensorReadings = new();
        private PlotModel _sensorPlotModel;
        private List<string> _availableZones = new();
        private string _selectedZone;
        private MqttService _mqttService;

        #endregion

        #region Properties

        private double? _latestPressurePsi;
        public double? LatestPressurePsi
        {
            get => _latestPressurePsi;
            set
            {
                if (_latestPressurePsi != value)
                {
                    _latestPressurePsi = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<StartTimeViewModel> StartTimes => Schedule.StartTimes;

        public ObservableCollection<SprinklerSet> VisibleSets
        {
            get => _visibleSets;
            set { _visibleSets = value; }
        }

        /// <summary>
        /// Bool property indicating if the system is watering or misting
        /// </summary>
        private bool _isWateringOrMisting;
        public bool IsWateringOrMisting
        {
            get => _isWateringOrMisting;
            set
            {
                if (_isWateringOrMisting != value)
                {
                    _isWateringOrMisting = value;
                    OnPropertyChanged(nameof(IsWateringOrMisting));
                }
            }
        }

        public Visibility IsWateringOrMistingVisible
        {
            get
            {
                if (IsWateringOrMisting)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        // Returns the current local time as a formatted string for UI binding
        public string CurrentTime => DateTime.Now.ToString("hh:mm:ss tt");

        public string SystemLedColor => LedColors != null && LedColors.TryGetValue("system", out var color) ? color : null;
        public ObservableCollection<ScheduledRunPreview> UpcomingRunsPreview { get; private set; } = new();
        public ObservableCollection<UpcomingRunInfo> UpcomingRuns
        {
            get => _upcomingRuns;
            set { _upcomingRuns = value; }
        }
        public ObservableCollection<string> PiStatusLog { get; private set; } = new();
        public WeatherViewModel WeatherVM { get; } = new WeatherViewModel();
        public SoilDataViewModel SoilDataVM { get; } = new SoilDataViewModel();
        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();
        public ObservableCollection<SprinklerSet> Sets => Schedule.Sets;
        public static ProgramEditorViewModel Current { get; private set; }
        public bool HasUnsavedChanges => _isDirty;

        /// <summary>
        /// True if wind speed is greater than 5 mph.
        /// </summary>
        public bool IsWindy
        {
            get => _isWindy;
            private set
            {
                if (_isWindy != value)
                {
                    _isWindy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsWindyVisible));
                }
            }
        }

        /// <summary>
        /// Gets or sets the visibility status indicating whether it is windy, greater than 5 mph wind.
        /// </summary>
        public Visibility IsWindyVisible
        {
            get
            {

                if (IsWindy)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }
        public bool IsSetSelected => SelectedSet != null;
        public bool IsStartTimeSelected => !string.IsNullOrEmpty(SelectedStartTime);
        public int TodayScheduleIndex
        {
            get => _todayScheduleIndex;
            set
            {
                if (_todayScheduleIndex != value)
                {
                    _todayScheduleIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        public Brush TodayIndexColor => IsTodayIndex(TodayScheduleIndex) ? Brushes.Red : Brushes.White;
        public string StatusIconPath
        {
            get => string.IsNullOrEmpty(_statusIconPath)
                ? "pack://application:,,,/Assets/Icons/unknown.png" // fallback image
                : _statusIconPath;
            set
            {
                if (_statusIconPath != value)
                {
                    _statusIconPath = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsSet1On { get => _isSet1On; set { _isSet1On = value; } }
        public bool IsSet2On { get => _isSet2On; set { _isSet2On = value; } }
        public bool IsSet3On { get => _isSet3On; set { _isSet3On = value; } }
        public Brush Set1Color { get => _set1Color; set { _set1Color = value; } }
        public Brush Set2Color { get => _set2Color; set { _set2Color = value; } }
        public Brush Set3Color { get => _set3Color; set { _set3Color = value; } }
        public bool IsTestMode
        {
            get => _isTestMode;
            set
            {
                if (_isTestMode != value)
                {
                    DebugLogger.LogVariableStatus($"IsTestMode SET: {_isTestMode} -> {value}");
                    _isTestMode = value;
                    _lastManualTestModeChange = DateTime.Now;
                    OnPropertyChanged();
                }
                else
                {
                    DebugLogger.LogVariableStatus($"IsTestMode unchanged: {_isTestMode}");
                }
            }
        }
        public bool PiReportedTestMode
        {
            get => _piReportedTestMode;
            set
            {
                if (_piReportedTestMode != value)
                {
                    _piReportedTestMode = value;
                    OnPropertyChanged();
                }
            }
        }
        public SprinklerSet? SelectedSet
        {
            get => _selectedSet;
            set
            {
                _selectedSet = value;
                // Update SelectedMapLine when SelectedSet changes
                if (_selectedSet != null)
                {
                    SelectedMapLine = SharedSprinklerLines.FirstOrDefault(
                        line => line.SprinklerLineTitle == _selectedSet.SetName);
                }
                else
                {
                    SelectedMapLine = null;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSetSelected));
            }
        }
        public string SelectedStartTime
        {
            get => _selectedStartTime;
            set
            {
                _selectedStartTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStartTimeSelected));
            }
        }
        public double SeasonalAdjustment
        {
            get => Schedule.SeasonalAdjustment;
            set
            {
                if (Schedule.SeasonalAdjustment != value)
                {
                    Schedule.SeasonalAdjustment = value;
                    foreach (var set in Sets)
                    {
                        set.OnPropertyChanged(nameof(set.SeasonallyAdjustedMinutes));
                    }
                    UpdateUpcomingRunsPreview();
                    DebouncedSaveAndSendToPi();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SeasonalAdjustmentPercent));
                }
            }
        }
        public int SeasonalAdjustmentPercent
        {
            get => (int)(SeasonalAdjustment * 100);
            set
            {
                SeasonalAdjustment = value / 100.0;
            }
        }

        // Week 1
        public bool Week1Sunday { get => Schedule.ScheduleDays[0]; set { Schedule.ScheduleDays[0] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Monday { get => Schedule.ScheduleDays[1]; set { Schedule.ScheduleDays[1] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Tuesday { get => Schedule.ScheduleDays[2]; set { Schedule.ScheduleDays[2] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Wednesday { get => Schedule.ScheduleDays[3]; set { Schedule.ScheduleDays[3] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Thursday { get => Schedule.ScheduleDays[4]; set { Schedule.ScheduleDays[4] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Friday { get => Schedule.ScheduleDays[5]; set { Schedule.ScheduleDays[5] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week1Saturday { get => Schedule.ScheduleDays[6]; set { Schedule.ScheduleDays[6] = value; DebouncedSaveAndSendToPi(); } }
        // Week 2
        public bool Week2Sunday { get => Schedule.ScheduleDays[7]; set { Schedule.ScheduleDays[7] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Monday { get => Schedule.ScheduleDays[8]; set { Schedule.ScheduleDays[8] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Tuesday { get => Schedule.ScheduleDays[9]; set { Schedule.ScheduleDays[9] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Wednesday { get => Schedule.ScheduleDays[10]; set { Schedule.ScheduleDays[10] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Thursday { get => Schedule.ScheduleDays[11]; set { Schedule.ScheduleDays[11] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Friday { get => Schedule.ScheduleDays[12]; set { Schedule.ScheduleDays[12] = value; DebouncedSaveAndSendToPi(); } }
        public bool Week2Saturday { get => Schedule.ScheduleDays[13]; set { Schedule.ScheduleDays[13] = value; DebouncedSaveAndSendToPi(); } }
        public string CurrentStation { get => _currentStation; set { if (_currentStation != value) { _currentStation = value; OnPropertyChanged(); } } }
        public string Countdown { get => _countdown; set { if (_countdown != value) { _countdown = value; OnPropertyChanged(); } } }
        public int? PiScheduleIndex { get => _piScheduleIndex; set { _piScheduleIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScheduleIndexMismatch)); } }
        public string PiLocalTime { get => _piLocalTime; set { _piLocalTime = value; OnPropertyChanged(); } }
        public string PiTimezone { get => _piTimezone; set { _piTimezone = value; OnPropertyChanged(); } }
        public bool ScheduleIndexMismatch => PiScheduleIndex != TodayScheduleIndex;
        public string SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection = value;
                    OnPropertyChanged(nameof(SelectedSection));
                }
            }
        }

        /*
# Possible values and structure for "system_status" in the API response:
#
# "system_status": "All Systems Nominal"
#   - This is the default/normal status indicating the controller is running without detected errors.
#
# Other possible values (if you implement or extend error/status reporting):
#   - "Manual Mode Active"         # If a manual run is in progress
#   - "Test Mode Enabled"          # If test mode is active (test_mode.txt == "1")
#   - "Error: <description>"       # If a critical error is detected (e.g., failed to load schedule)
#   - "Maintenance Mode"           # If you add a maintenance flag/feature
#   - "Startup"                    # During initial boot or startup sequence
#
# The "system_status" field is a string and is included in the JSON response from the /status endpoint.
# Example:
# {
#   "system_status": "All Systems Nominal",
#   ...
# }
        */

        public string SystemStatus
        {
            get => _systemStatus;
            set
            {
                _systemStatus = value;
                UpdateStatusIcon(_systemStatus);
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ZoneStatus> ZoneStatuses
        {
            get => _zoneStatuses;
            set { _zoneStatuses = value; OnPropertyChanged(); }
        }
        public string NextRunDisplay
        {
            get
            {
                if (NextRun == null)
                    return "-";
                // Try to parse the start time as a DateTime
                if (DateTime.TryParse(NextRun.StartTime, out var dt))
                {
                    return $"{dt:MMM dd, yyyy 'at' h:mm tt} - {NextRun.Set} ({NextRun.DurationMinutes} min)";
                }
                // Fallback to original string if parsing fails
                return $"{NextRun.StartTime} - {NextRun.Set} ({NextRun.DurationMinutes} min)";
            }
        }
        public string LastRunDisplay
        {
            get
            {
                if (LastCompletedRun != null)
                {
                    var endTime = DateTime.TryParse(LastCompletedRun.EndTime, out var dt)
                        ? dt.ToString("MMM dd, yyyy 'at' h:mm tt")
                        : LastCompletedRun.EndTime;
                    return $"{endTime} - {LastCompletedRun.Set} ({LastCompletedRun.DurationMinutes} min, {LastCompletedRun.Status})";
                }
                return _lastRunDisplay;
            }
            set { _lastRunDisplay = value; OnPropertyChanged(); }
        }
        public ObservableCollection<WateringLogEntry> LastWeekHistory
        {
            get => _lastWeekHistory;
            set { _lastWeekHistory = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BackyardBoss.Models.MistSettingViewModel> MistSettingsCollection => Schedule.Mist.TemperatureSettings;
        public Dictionary<string, string> LedColors
        {
            get => _ledColors;
            set { _ledColors = value; OnPropertyChanged(); OnPropertyChanged(nameof(SystemLedColor)); }
        }
        public PiRunInfo CurrentRun
        {
            get => _currentRun;
            set
            {
                _currentRun = value;
                UpdateStatusIcon(SystemStatus);
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentRunDisplay));
            }
        }
        public PiRunInfo NextRun
        {
            get => _nextRun;
            set { _nextRun = value; UpdateStatusIcon(SystemStatus); OnPropertyChanged(); OnPropertyChanged(nameof(NextRunDisplay)); }
        }
        public LastCompletedRunInfo LastCompletedRun
        {
            get => _lastCompletedRun;
            set { _lastCompletedRun = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastRunDisplay)); }
        }
        public string CurrentRunDisplay
        {
            get
            {
                if (SystemStatus != null && SystemStatus.Contains("Offline", StringComparison.OrdinalIgnoreCase))
                    return "Offline";
                if (CurrentRun == null)
                    return "Idle";
                string phase = CurrentRun.Phase ?? string.Empty;
                if (phase.Equals("Soaking", StringComparison.OrdinalIgnoreCase) && CurrentRun.SoakRemainingSec.HasValue)
                {
                    int sec = CurrentRun.SoakRemainingSec.Value;
                    int sec2 = CurrentRun.TimeRemainingSec.Value;
                    return $"{CurrentRun.Set} Soak ({sec / 60:D2}:{sec % 60:D2} -> {sec2 / 60:D2}:{sec2 % 60:D2})";
                }
                else if (CurrentRun.TimeRemainingSec.HasValue)
                {
                    int sec = CurrentRun.TimeRemainingSec.Value;
                    return $"{CurrentRun.Set} {CurrentRun.Phase} ({sec / 60:D2}:{sec % 60:D2})";
                }
                else
                {
                    return $"{CurrentRun.Set} {CurrentRun.Phase}";
                }
            }
        }
        public BackyardBoss.Models.MistStatus MistStatus
        {
            get => _mistStatus;
            set { _mistStatus = value; OnPropertyChanged(); }
        }
        public ImageSource MapImageSource
        {
            get => _mapImageSource;
            set { _mapImageSource = value; OnPropertyChanged(); }
        }
        public ObservableCollection<SprinklerLineModel> SharedSprinklerLines
        {
            get => _sharedSprinklerLines;
            set { _sharedSprinklerLines = value; OnPropertyChanged(); }
        }
        public SprinklerLineModel? SelectedMapLine
        {
            get => _selectedMapLine;
            set
            {
                if (_selectedMapLine != value)
                {
                    _selectedMapLine = value;
                    OnPropertyChanged(nameof(SelectedMapLine));
                }
            }
        }
        public ObservableCollection<SensorReading> SensorReadings
        {
            get => _sensorReadings;
            set { _sensorReadings = value; OnPropertyChanged(); }
        }
        public PlotModel SensorPlotModel
        {
            get => _sensorPlotModel;
            set { _sensorPlotModel = value; OnPropertyChanged(); }
        }
        public List<string> AvailableZones
        {
            get => _availableZones;
            set { _availableZones = value; OnPropertyChanged(); }
        }
        public string SelectedZone
        {
            get => _selectedZone;
            set { _selectedZone = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EnvironmentData> EnvironmentReadings { get; set; } = new();
        public ObservableCollection<PlantData> PlantReadings { get; set; } = new();
        public ObservableCollection<SetsData> SetsReadings { get; set; } = new();
        public ObservableCollection<PressureAvgData> PressureAvgHistory { get; set; } = new();
        #endregion

        #region Commands
        public ICommand OpenTimePickerCommand { get; }
        public ICommand SaveScheduleCommand { get; }
        public ICommand AddSetCommand { get; }
        public ICommand RemoveSetCommand { get; }
        public ICommand AddStartTimeCommand { get; }
        public ICommand RemoveStartTimeCommand { get; }
        public ICommand RunOnceCommand { get; }
        public ICommand SaveAndSendCommand { get; }
        public ICommand CopyProject { get; }
        public ICommand QuickMistCommand { get; }
        public ICommand RefreshPiStatusCommand { get; }
        public ICommand ShowPiLogCommand { get; }
        public ICommand ShowPlotsCommand { get; }
        public ICommand ToggleTestModeCommand { get; }
        public ICommand StopAllCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand ReloadCommand { get; }
        #endregion

        #region Constructor
        public ProgramEditorViewModel()
        {
            DebugLogger.LogVariableStatus("Constructor initialized.");
            Current = this;
            WeatherVM = new WeatherViewModel();
            WeatherVM.PropertyChanged += WeatherVM_PropertyChanged;
            _ = WeatherVM.LoadWeatherAsync();
            UpdateIsWindy(); // Initial check
            CalculateTodayScheduleIndex();
            LoadSchedule(); // <-- Enable real data loading
            // Debounce timer for saving and uploading schedule
            _debounceSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _debounceSaveTimer.Tick += (s, e) =>
            {
                _debounceSaveTimer.Stop();
                Save(SaveTarget.LocalAndSendToPi);
            };
            _ = LoadPressureHistoryAsync();
            UpdatePressureAvgPlot();
            // Subscribe to PressureAvgHistory changes to update plot
            PressureAvgHistory.CollectionChanged += (s, e) => UpdatePressureAvgPlot();
            // Populate mock data for dashboard demo
            SystemStatus = "All Systems Nominal";
            var demoSetNames = new[] { "Front Lawn", "Backyard", "Garden" };
            ZoneStatuses = new ObservableCollection<ZoneStatus>
            {
                new ZoneStatus { Name = demoSetNames[0], Status = "Idle" },
                new ZoneStatus { Name = demoSetNames[1], Status = "Watering" },
                new ZoneStatus { Name = demoSetNames[2], Status = "Idle" }
            };
            LastRunDisplay = $"{DateTime.Now.AddHours(-2):yyyy-MM-dd HH:mm} - {demoSetNames[0]} (10 min)";
            UpcomingRunsPreview = new ObservableCollection<ScheduledRunPreview>
            {
                new ScheduledRunPreview { SetName = demoSetNames[1], StartTime = DateTime.Now.AddHours(1).ToString("HH:mm"), SeasonallyAdjustedMinutes = 12 },
                new ScheduledRunPreview { SetName = demoSetNames[2], StartTime = DateTime.Now.AddHours(2).ToString("HH:mm"), SeasonallyAdjustedMinutes = 15 }
            };
            LastWeekHistory = new ObservableCollection<WateringLogEntry>
            {
                new WateringLogEntry { Date = DateTime.Today.AddDays(-1), SetName = demoSetNames[0], DurationMinutes = 10, Status = "Completed" },
                new WateringLogEntry { Date = DateTime.Today.AddDays(-2), SetName = demoSetNames[1], DurationMinutes = 12, Status = "Completed" },
                new WateringLogEntry { Date = DateTime.Today.AddDays(-3), SetName = demoSetNames[2], DurationMinutes = 15, Status = "Error" }
            };
            // Only keep time display update
            _timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeTimer.Start();
            // Example: Load a default map image and demo lines
            MapImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Map/default_map.png"));
            SharedSprinklerLines = new ObservableCollection<BackyardBoss.UserControls.SprinklerLineModel>
            {
                new BackyardBoss.UserControls.SprinklerLineModel
                {
                    SprinklerLineTitle = "Front Lawn",
                    DefaultColor = (Color)ColorConverter.ConvertFromString("#0033A0"),
                    IsWatering = true,
                    Points = new ObservableCollection<BackyardBoss.UserControls.SprinklerLinePoint>
                    {
                        new BackyardBoss.UserControls.SprinklerLinePoint { X = 100, Y = 100, Type = BackyardBoss.UserControls.PointType.Sprinkler },
                        new BackyardBoss.UserControls.SprinklerLinePoint { X = 200, Y = 200, Type = BackyardBoss.UserControls.PointType.TConnection }
                    },
                    Connections = new ObservableCollection<BackyardBoss.UserControls.SprinklerLineConnection>
                    {
                        new BackyardBoss.UserControls.SprinklerLineConnection { FromPointId = Guid.Empty, ToPointId = Guid.Empty } // will set below
                    }
                },
                new BackyardBoss.UserControls.SprinklerLineModel
                {
                    SprinklerLineTitle = "Backyard",
                    DefaultColor = (Color)ColorConverter.ConvertFromString("#FF6200"),
                    IsWatering = false,
                    Points = new ObservableCollection<BackyardBoss.UserControls.SprinklerLinePoint>
                    {
                        new BackyardBoss.UserControls.SprinklerLinePoint { X = 300, Y = 100, Type = BackyardBoss.UserControls.PointType.Mister },
                        new BackyardBoss.UserControls.SprinklerLinePoint { X = 400, Y = 200, Type = BackyardBoss.UserControls.PointType.Dripper }
                    },
                    Connections = new ObservableCollection<BackyardBoss.UserControls.SprinklerLineConnection>
                    {
                        new BackyardBoss.UserControls.SprinklerLineConnection { FromPointId = Guid.Empty, ToPointId = Guid.Empty } // will set below
                    }
                }
            };
            // Fix up connection IDs to match the actual point Guids
            foreach (var line in SharedSprinklerLines)
            {
                if (line.Points.Count > 1 && line.Connections.Count > 0)
                {
                    line.Connections[0].FromPointId = line.Points[0].Id;
                    line.Connections[0].ToPointId = line.Points[1].Id;
                }
            }
            UpdateVisibleSets();
            Sets.CollectionChanged += (s, e) => UpdateVisibleSets();
            // Load map from JSON
            LoadMapFromJson();
            // MQTT initialization and data handling
            _mqttService = new MqttService();
            _mqttService.MessageReceived += (topic, json) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Console.WriteLine($"Received MQTT message on topic: {topic}");
                        if (topic == "sensors/environment")
                        {
                            var data = JsonSerializer.Deserialize<EnvironmentData>(json.GetRawText());
                            if (data != null)
                            {
                                EnvironmentReadings.Add(data);
                                // Debug: Log the received wind speed
                                Debug.WriteLine($"[MQTT] Received EnvWindSpeed: {data.WindSpeed}");
                                // Update WeatherVM.EnvWindSpeed, EnvHumidity, EnvTemperature
                                Application.Current.Dispatcher.Invoke(() => {
                                    WeatherVM.EnvWindSpeed = data.WindSpeed.ToString("F1") + " mph";
                                    WeatherVM.EnvHumidity = data.Humidity.ToString("F0") + "%";
                                    double tempF = data.Temperature * 9.0 / 5.0 + 32.0;
                                    WeatherVM.EnvTemperature = tempF.ToString("F1") + "°F";
                                    // Debug: Log the set value
                                    Debug.WriteLine($"[UI] Set WeatherVM.EnvWindSpeed: {WeatherVM.EnvWindSpeed}");
                                });
                            }
                        }
                        else if (topic == "sensors/plant")
                        {
                            var data = JsonSerializer.Deserialize<PlantData>(json.GetRawText());
                            if (data != null) PlantReadings.Add(data);
                        }
                        else if (topic == "sensors/sets")
                        {
                            var data = JsonSerializer.Deserialize<SetsData>(json.GetRawText());
                            if (data != null) SetsReadings.Add(data);
                            LatestPressurePsi = data.PressurePsi;
                        }
                        else if (topic == "status/watering")
                        {
                            var status = JsonSerializer.Deserialize<PiStatusResponse>(json.GetRawText());
                            if (status != null)
                            {
                                SystemStatus = status.SystemStatus;
                                PiReportedTestMode = status.TestMode;
                                LedColors = status.LedColors ?? new Dictionary<string, string>();
                                ZoneStatuses = new ObservableCollection<ZoneStatus>(status.Zones ?? new List<ZoneStatus>());
                                CurrentRun = status.CurrentRun;
                                NextRun = status.NextRun;
                                LastCompletedRun = status.LastCompletedRun;
                                UpcomingRuns = new ObservableCollection<UpcomingRunInfo>(status.UpcomingRuns ?? new List<UpcomingRunInfo>());
                                MistStatus = status.MistStatus; // Now assign MistStatus from status
                            }
                        }
                    }
                    catch { /* Optionally log/handle error */ }
                });
            };
            _ = _mqttService.ConnectAsync(); // Use default broker address (100.116.147.6)

            // Initialize SelectSectionCommand
            SelectSectionCommand = new RelayCommand(param =>
            {
                if (param is string section && !string.IsNullOrWhiteSpace(section))
                {
                    SelectedSection = section;
                }
            });
        }
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void WeatherVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WeatherViewModel.WindSpeed))
            {
                UpdateIsWindy();
            }
        }

        private void UpdateIsWindy()
        {
            // Try to parse wind speed as double (mph)
            if (double.TryParse(WeatherVM.WindSpeed, out var windMph))
                IsWindy = windMph > 5.0;
            else
                IsWindy = false;
        }

        #region Schedule Management
        private void CalculateTodayScheduleIndex()
        {
            //var baseDate = new DateTime(2024, 1, 1);
            var baseDate = new DateTime(2023, 12, 31); // Sunday
            var today = DateTime.Today;
            var deltaDays = (today - baseDate).Days;
            TodayScheduleIndex = deltaDays % 14;
            DebugLogger.LogVariableStatus($"TodayScheduleIndex calculated: {TodayScheduleIndex}");
        }
        public bool IsTodayIndex(int index) => index == TodayScheduleIndex;
        private async void LoadSchedule()
        {
            DebugLogger.LogFileIO($"Today (Local): {DateTime.Today:yyyy-MM-dd}");
            DebugLogger.LogFileIO($"Today (UTC):   {DateTime.UtcNow.Date:yyyy-MM-dd}");
            DebugLogger.LogFileIO("Loading schedule...");
            _suppressExport = true;
            var loaded = await ProgramDataService.LoadScheduleAsync();
            if (loaded != null)
            {
                // Debug: Print mist settings from loaded JSON
                if (loaded.Mist != null && loaded.Mist.TemperatureSettings != null && loaded.Mist.TemperatureSettings.Count > 0)
                {
                    DebugLogger.LogFileIO($"Loaded.Mist.TemperatureSettings.Count: {loaded.Mist.TemperatureSettings.Count}");
                    int idx = 0;
                    foreach (var m in loaded.Mist.TemperatureSettings)
                    {
                        DebugLogger.LogFileIO($"[{idx}] Temp: {m.Temperature}, Interval: {m.Interval}, Duration: {m.Duration}");
                        DebugLogger.LogFileIO($"    Type: {m.GetType().FullName}");
                        DebugLogger.LogFileIO($"    Assembly: {m.GetType().AssemblyQualifiedName}");
                        idx++;
                    }
                }
                else
                {
                    DebugLogger.LogFileIO("Loaded.Mist or TemperatureSettings is null or empty");
                }
                Schedule = loaded;
                if (loaded.ScheduleDays == null || loaded.ScheduleDays.Count != 14)
                {
                    DebugLogger.LogFileIO("Initializing blank ScheduleDays array.");
                    loaded.ScheduleDays = new ObservableCollection<bool>(Enumerable.Repeat(false, 14));
                }
                if (loaded.Mist == null)
                {
                    DebugLogger.LogFileIO("Mist section missing — initializing defaults.");
                    loaded.Mist = new BackyardBoss.Models.MistSettings();
                }
                if (loaded.Mist.TemperatureSettings == null || loaded.Mist.TemperatureSettings.Count == 0)
                {
                    DebugLogger.LogFileIO("TemperatureSettings missing — initializing defaults.");
                    loaded.Mist.TemperatureSettings = new System.Collections.ObjectModel.ObservableCollection<BackyardBoss.Models.MistSettingViewModel>
                    {
                        new BackyardBoss.Models.MistSettingViewModel { Temperature = 90, Interval = 20, Duration = 2 },
                        new BackyardBoss.Models.MistSettingViewModel { Temperature = 95, Interval = 20, Duration = 2 },
                        new BackyardBoss.Models.MistSettingViewModel { Temperature = 100, Interval = 20, Duration = 2 }
                    };
                }
                if (loaded.StartTimes == null || loaded.StartTimes.Count == 0)
                {
                    DebugLogger.LogFileIO("No start times found — inserting default 06:00 and 17:00.");
                    loaded.StartTimes = new ObservableCollection<StartTimeViewModel>
                    {
                        new StartTimeViewModel { Time = "06:00", IsEnabled = true },
                        new StartTimeViewModel { Time = "17:00", IsEnabled = true }
                    };
                }
                UpdateVisibleSets();
                _isDirty = false;
                DebugLogger.LogFileIO("Schedule loaded successfully.");
                UpdateUpcomingRunsPreview();
            }
            else
            {
                DebugLogger.LogFileIO("Failed to load schedule.");
            }
            _suppressExport = false;
        }
        public void AutoSave()
        {
            if (_suppressExport)
            {
                DebugLogger.LogAutoSave("AutoSave skipped (suppressed during load).");
                return;
            }
            DebugLogger.LogAutoSave("AutoSave called.");
            MarkDirty();
            Save(SaveTarget.LocalOnly);
        }
        public void DebouncedSaveAndSendToPi()
        {
            if (_suppressExport)
            {
                DebugLogger.LogAutoSave("DebouncedSaveAndSendToPi skipped (suppressed during load).");
                return;
            }
            DebugLogger.LogAutoSave("DebouncedSaveAndSendToPi called.");
            MarkDirty();
            _debounceSaveTimer.Stop();
            _debounceSaveTimer.Start();
        }
        private void AddSet()
        {
            var newSet = new SprinklerSet { SetName = "New Set", RunDurationMinutes = 10 };
            Sets.Add(newSet);
            DebugLogger.LogVariableStatus($"Set added: {newSet.SetName}, {newSet.RunDurationMinutes} min.");
            DebouncedSaveAndSendToPi();
            UpdateUpcomingRunsPreview();
            UpdateVisibleSets();
        }
        private void RemoveSet()
        {
            if (SelectedSet != null)
            {
                DebugLogger.LogVariableStatus($"Removing set: {SelectedSet.SetName}");
                Sets.Remove(SelectedSet);
                SelectedSet = null;
                DebouncedSaveAndSendToPi();
                UpdateUpcomingRunsPreview();
                UpdateVisibleSets();
            }
            else
            {
                DebugLogger.LogVariableStatus("RemoveSet called, but no set was selected.");
            }
        }
        private void AddStartTime()
        {
            var time = new StartTimeViewModel { Time = "06:00" };
            StartTimes.Add(time);
            DebugLogger.LogVariableStatus($"Start time added: {time.Time}");
            DebouncedSaveAndSendToPi();
            UpdateUpcomingRunsPreview();
        }
        private void RemoveStartTime()
        {
            if (StartTimes.Count > 0)
            {
                var removed = StartTimes.Last();
                StartTimes.Remove(removed);
                DebugLogger.LogVariableStatus($"Start time removed: {removed.Time}");
                DebouncedSaveAndSendToPi();
                UpdateUpcomingRunsPreview();
            }
            else
            {
                DebugLogger.LogVariableStatus("RemoveStartTime called but list was empty.");
            }
        }
        public void SortStartTimes()
        {
            DebugLogger.LogVariableStatus("Sorting start times.");
            var sorted = StartTimes.OrderBy(t => TimeSpan.Parse(t.Time)).ToList();
            StartTimes.Clear();
            foreach (var t in sorted)
            {
                DebugLogger.LogVariableStatus($" - {t.Time}");
                StartTimes.Add(t);
            }
            DebouncedSaveAndSendToPi();
            UpdateUpcomingRunsPreview();
        }
        public void UpdateUpcomingRunsPreview()
        {
            DebugLogger.LogVariableStatus("Updating upcoming runs preview.");
            UpcomingRunsPreview.Clear();
            var now = DateTime.Now.TimeOfDay;
            foreach (var start in StartTimes)
            {
                var programStartTime = start.ParsedTime;
                if (programStartTime >= now)
                {
                    var cumulative = programStartTime;
                    foreach (var set in Sets)
                    {
                        int adjusted = (int)Math.Round(set.RunDurationMinutes * SeasonalAdjustment);
                        UpcomingRunsPreview.Add(new ScheduledRunPreview
                        {
                            SetName = set.SetName,
                            StartTime = cumulative.ToString(@"hh\:mm"),
                            RunDurationMinutes = set.RunDurationMinutes,
                            SeasonallyAdjustedMinutes = adjusted
                        });
                        cumulative = cumulative.Add(TimeSpan.FromMinutes(adjusted));
                    }
                }
            }
            var sorted = UpcomingRunsPreview.OrderBy(r => TimeSpan.Parse(r.StartTime)).ToList();
            UpcomingRunsPreview.Clear();
            foreach (var r in sorted)
                UpcomingRunsPreview.Add(r);
        }
        private void MarkDirty()
        {
            DebugLogger.LogAutoSave("MarkDirty called.");
            _isDirty = true;
        }
        private void UpdateVisibleSets()
        {
            var filtered = Sets.Where(s => !s.SetName.Equals("Misters", StringComparison.OrdinalIgnoreCase)).ToList();
            VisibleSets.Clear();
            foreach (var set in filtered)
                VisibleSets.Add(set);
        }
        #endregion

        #region Run Management
        private void RunOnce()
        {
            var dialog = new RunOnceDialog(Sets);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var selected = dialog.GetOverrides().Where(s => s.Duration > 0).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("No durations selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                DebugLogger.LogVariableStatus("RunOnce: Executing the following sets:");
                foreach (var run in selected)
                {
                    DebugLogger.LogVariableStatus($" - {run.SetName}: {run.Duration} min");
                }
                var command = new
                {
                    manual_run = new
                    {
                        sets = selected.Select(s => s.SetName).ToArray(),
                        duration_minutes = selected.Max(s => s.Duration)
                    }
                };
                string localPath = "manual_command.json";
                string remotePath = "/home/lds00/sprinkler/manual_command.json";
                string piHost = "100.116.147.6";
                string username = "lds00";
                string password = "Celica1!";
                try
                {
                    File.WriteAllText(localPath, JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true }));
                    using var sftp = new SftpClient(piHost, username, password);
                    sftp.Connect();
                    using var stream = File.OpenRead(localPath);
                    sftp.UploadFile(stream, remotePath, true);
                    sftp.Disconnect();
                    MessageBox.Show("RunOnce command sent to Raspberry Pi.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send RunOnce command: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void SendManualRun(string setName, int durationMinutes)
        {
            var command = new
            {
                manual_run = new
                {
                    sets = new[] { setName },
                    duration_minutes = durationMinutes
                }
            };
            string localPath = "manual_command.json";
            string remotePath = "/home/lds00/sprinkler/manual_command.json";
            string piHost = "100.116.147.6";
            string username = "lds00";
            string password = "Celica1!";
            try
            {
                File.WriteAllText(localPath, JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true }));
                using var sftp = new SftpClient(piHost, username, password);
                sftp.Connect();
                using var stream = File.OpenRead(localPath);
                sftp.UploadFile(stream, remotePath, true);
                sftp.Disconnect();
                MessageBox.Show($"Manual run sent: {setName} for {durationMinutes} min.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send manual run: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region UI Helpers
        private void UpdateStatusIcon(string status)
        {
            // Show "running" if a run is in progress
            if (CurrentRun != null && !string.IsNullOrEmpty(CurrentRun.Phase))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/running.png";
                return;
            }

            // Show "offline" if system is offline
            if (!string.IsNullOrEmpty(status) && status.Contains("Offline", StringComparison.OrdinalIgnoreCase))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/offline.png";
                return;
            }

            // Show "test mode" if in test mode
            if (PiReportedTestMode)
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/testmode.png";
                return;
            }

            // Show "error" if system status contains error
            if (!string.IsNullOrEmpty(status) && status.Contains("Error", StringComparison.OrdinalIgnoreCase))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/error.png";
                return;
            }

            // Show "idle" if system is nominal or idle
            if (!string.IsNullOrEmpty(status) &&
                (status.Contains("Nominal", StringComparison.OrdinalIgnoreCase) ||
                 status.Contains("Idle", StringComparison.OrdinalIgnoreCase) ||
                 status.Contains("Ready", StringComparison.OrdinalIgnoreCase)))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/idle.png";
                return;
            }

            // Fallback: unknown status
            StatusIconPath = "pack://application:,,,/Assets/Icons/unknown.png";
        }
        private void ToggleTestMode()
        {
            bool newValue = !PiReportedTestMode;
            DebugLogger.LogVariableStatus($"ToggleTestMode called. PiReportedTestMode={PiReportedTestMode}, sending newValue={newValue}");
        }
        private void OpenPlotWindow()
        {
            var window = new WateringHistoryView();
            window.Show();
        }
        private void CopyProject2Clip()
        {
            string rootDirectory = @"C:\Users\lds00\source\repos\WpfApp1\WpfApp1\";
            BackyardBoss.Tools.ProjectStructureToClipboard.ExportStructureWithContents(rootDirectory);
        }
        #endregion

        #region Save Management
        public enum SaveTarget { LocalOnly, LocalAndSendToPi }
        private void Save(SaveTarget target)
        {
            lock (_saveLock)
            {
                DebugLogger.LogFileIO("Save triggered.");
                Schedule.StartTimes = new ObservableCollection<StartTimeViewModel>(StartTimes);
                foreach (var set in Schedule.Sets)
                {
                    if (set.SetName == "Misters")
                        set.Mode = true;
                }
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                };
                string localPath = "sprinkler_schedule.json";
                string remotePath = "/home/lds00/sprinkler/sprinkler_schedule.json";
                string piHost = "100.116.147.6";
                string username = "lds00";
                string password = "Celica1!";
                try
                {
                    var json = JsonSerializer.Serialize(Schedule, options);
                    using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(json);
                    }
                    if (target == SaveTarget.LocalAndSendToPi)
                    {
                        using var sftp = new SftpClient(piHost, username, password);
                        sftp.Connect();
                        using var stream = File.OpenRead(localPath);
                        sftp.UploadFile(stream, remotePath, true);
                        sftp.Disconnect();
                        MessageBox.Show("Schedule saved and sent to Raspberry Pi.");
                    }
                    else
                    {
                        DebugLogger.LogFileIO("Schedule saved locally only.");
                    }
                    _isDirty = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save: {ex.Message}");
                }
            }
        }
        #endregion

        #region Map Management
        private void LoadMapFromJson()
        {
            try
            {
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "myMap.json");
                if (!File.Exists(jsonPath)) return;
                var json = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var lines = JsonSerializer.Deserialize<ObservableCollection<SprinklerLineModel>>(json, options);
                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (line.DefaultColor == default)
                            line.DefaultColor = Colors.Blue;
                    }
                    SharedSprinklerLines = lines;
                    // Default to first line selected
                    SelectedMapLine = SharedSprinklerLines.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load map: {ex.Message}");
            }
        }
        #endregion

        #region Pressure Plot Management
        public void UpdatePressureAvgPlot()
        {
            // Preserve current zoom/pan
            double? xMin = null, xMax = null, yMin = null, yMax = null;
            if (SensorPlotModel != null)
            {
                var xAxis = SensorPlotModel.Axes.OfType<DateTimeAxis>().FirstOrDefault();
                var yAxis = SensorPlotModel.Axes.OfType<LinearAxis>().FirstOrDefault();
                if (xAxis != null)
                {
                    xMin = xAxis.ActualMinimum;
                    xMax = xAxis.ActualMaximum;
                }
                if (yAxis != null)
                {
                    yMin = yAxis.ActualMinimum;
                    yMax = yAxis.ActualMaximum;
                }
            }

            var model = new PlotModel { Title = "Pressure (5-min Avg, All Data)" };
            var series = new LineSeries { Title = "Avg Pressure (PSI)", MarkerType = MarkerType.Circle };
            foreach (var data in PressureAvgHistory.OrderBy(d => d.Timestamp))
            {
                var x = DateTimeAxis.ToDouble(data.Timestamp);
                var y = data.AvgPressurePsi;
                Debug.WriteLine($"Plot point: Timestamp={data.Timestamp}, X={x}, Y={y}");
                series.Points.Add(new DataPoint(x, y));
            }

            Debug.WriteLine($"Total series points: {series.Points.Count}");
            if (series.Points.Count > 0)
            {
                Debug.WriteLine($"First point: X={series.Points[0].X}, Y={series.Points[0].Y}");
                Debug.WriteLine($"Last point: X={series.Points[^1].X}, Y={series.Points[^1].Y}");
            }

            Debug.WriteLine(series.Points.Count);

            model.Series.Add(series);
            var xAxisNew = new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM-dd HH:mm", Title = "Time" };
            var yAxisNew = new LinearAxis { Position = AxisPosition.Left, Title = "Pressure (PSI)" };
            model.Axes.Add(xAxisNew);
            model.Axes.Add(yAxisNew);

            /*
            // Restore previous zoom/pan
            if (xMin.HasValue && xMax.HasValue)
                xAxisNew.Zoom(xMin.Value, xMax.Value);
            if (yMin.HasValue && yMax.HasValue)
                yAxisNew.Zoom(yMin.Value, yMax.Value);
            */
            SensorPlotModel = model;
            OnPropertyChanged(nameof(SensorPlotModel)); // Ensure property change notification
        }

        private async Task LoadPressureHistoryAsync()
        {
            try
            {
                var repo = new BackyardBoss.Data.SqliteSensorDataRepository("pressure_data.db");
                var history = await repo.GetPressureAvg5MinAggregatesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PressureAvgHistory.Clear();
                    foreach (var item in history)
                        PressureAvgHistory.Add(new PressureAvgData
                        {
                            Timestamp = item.Timestamp,
                            AvgPressurePsi = item.AvgPressurePsi,
                            NumSamples = item.NumSamples,
                            Version = item.Version
                        });
                });


            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load pressure history: {ex.Message}");
            }
        }
        #endregion
    }
}