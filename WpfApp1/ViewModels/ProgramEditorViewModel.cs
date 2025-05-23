﻿// ProgramEditorViewModel with verbose auto-save logging
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BackyardBoss.Commands;
using BackyardBoss.Dialogs;
using BackyardBoss.Models;
using BackyardBoss.Services;
using BackyardBoss.Views;
using Renci.SshNet;
using WpfApp1;
using BackyardBoss.ViewModels;
using System.Windows.Media;
using System.Text.Json.Serialization;

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
        #endregion

        #region Properties
        public ObservableCollection<StartTimeViewModel> StartTimes => Schedule.StartTimes;
        
        public ObservableCollection<SprinklerSet> VisibleSets
        {
            get
            {

               return new ObservableCollection<SprinklerSet>(Sets.Where(s => !s.SetName.Equals("Misters", StringComparison.OrdinalIgnoreCase)));
            }
        }
        
        public ObservableCollection<ScheduledRunPreview> UpcomingRuns { get; private set; } = new();
        public ObservableCollection<string> PiStatusLog { get; private set; } = new();
        public WeatherViewModel WeatherVM { get; } = new WeatherViewModel();
        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();
        public ObservableCollection<SprinklerSet> Sets => Schedule.Sets;
        public static ProgramEditorViewModel Current { get; private set; }
        public bool HasUnsavedChanges => _isDirty;
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
            get => _statusIconPath;
            set
            {
                if (_statusIconPath != value)
                {
                    _statusIconPath = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsSet1On { get => _isSet1On; set { _isSet1On = value; OnPropertyChanged(); } }
        public bool IsSet2On { get => _isSet2On; set { _isSet2On = value; OnPropertyChanged(); } }
        public bool IsSet3On { get => _isSet3On; set { _isSet3On = value; OnPropertyChanged(); } }
        public Brush Set1Color { get => _set1Color; set { _set1Color = value; OnPropertyChanged(); } }
        public Brush Set2Color { get => _set2Color; set { _set2Color = value; OnPropertyChanged(); } }
        public Brush Set3Color { get => _set3Color; set { _set3Color = value; OnPropertyChanged(); } }
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
                    SetEnvironmentVariable("TEST_MODE", value ? "1" : "0");
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
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SeasonalAdjustment));
                    OnPropertyChanged(nameof(SeasonalAdjustmentPercent));
                    foreach (var set in Sets)
                    {
                        set.OnPropertyChanged(nameof(set.SeasonallyAdjustedMinutes));
                    }
                    UpdateUpcomingRunsPreview();
                    DebouncedSaveAndSendToPi();
                }
            }
        }
        public int SeasonalAdjustmentPercent
        {
            get => (int)(SeasonalAdjustment * 100);
            set
            {
                SeasonalAdjustment = value / 100.0;
                OnPropertyChanged();
            }
        }

        // Week 1
        public bool Week1Sunday { get => Schedule.ScheduleDays[0]; set { Schedule.ScheduleDays[0] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Monday { get => Schedule.ScheduleDays[1]; set { Schedule.ScheduleDays[1] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Tuesday { get => Schedule.ScheduleDays[2]; set { Schedule.ScheduleDays[2] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Wednesday { get => Schedule.ScheduleDays[3]; set { Schedule.ScheduleDays[3] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Thursday { get => Schedule.ScheduleDays[4]; set { Schedule.ScheduleDays[4] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Friday { get => Schedule.ScheduleDays[5]; set { Schedule.ScheduleDays[5] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week1Saturday { get => Schedule.ScheduleDays[6]; set { Schedule.ScheduleDays[6] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        // Week 2
        public bool Week2Sunday { get => Schedule.ScheduleDays[7]; set { Schedule.ScheduleDays[7] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Monday { get => Schedule.ScheduleDays[8]; set { Schedule.ScheduleDays[8] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Tuesday { get => Schedule.ScheduleDays[9]; set { Schedule.ScheduleDays[9] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Wednesday { get => Schedule.ScheduleDays[10]; set { Schedule.ScheduleDays[10] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Thursday { get => Schedule.ScheduleDays[11]; set { Schedule.ScheduleDays[11] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Friday { get => Schedule.ScheduleDays[12]; set { Schedule.ScheduleDays[12] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public bool Week2Saturday { get => Schedule.ScheduleDays[13]; set { Schedule.ScheduleDays[13] = value; OnPropertyChanged(); DebouncedSaveAndSendToPi(); } }
        public string CurrentStation { get => _currentStation; set { if (_currentStation != value) { _currentStation = value; OnPropertyChanged(); } } }
        public string Countdown { get => _countdown; set { if (_countdown != value) { _countdown = value; OnPropertyChanged(); } } }
        public int? PiScheduleIndex { get => _piScheduleIndex; set { _piScheduleIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScheduleIndexMismatch)); } }
        public string PiLocalTime { get => _piLocalTime; set { _piLocalTime = value; OnPropertyChanged(); } }
        public string PiTimezone { get => _piTimezone; set { _piTimezone = value; OnPropertyChanged(); } }
        public bool ScheduleIndexMismatch => PiScheduleIndex != TodayScheduleIndex;
        public string SelectedSection
        {
            get => _selectedSection;
            set { _selectedSection = value; OnPropertyChanged(); }
        }
        public string SystemStatus
        {
            get => _systemStatus;
            set { _systemStatus = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ZoneStatus> ZoneStatuses
        {
            get => _zoneStatuses;
            set { _zoneStatuses = value; OnPropertyChanged(); }
        }
        public string NextRunDisplay => NextRun == null ? "-" : $"{NextRun.StartTime} - {NextRun.Set} ({NextRun.DurationMinutes} min)";
        public string LastRunDisplay
        {
            get => _lastRunDisplay;
            set { _lastRunDisplay = value; OnPropertyChanged(); }
        }
        public ObservableCollection<WateringLogEntry> LastWeekHistory
        {
            get => _lastWeekHistory;
            set { _lastWeekHistory = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MistSettingViewModel> MistSettingsCollection => Schedule.Mist.TemperatureSettings;
        public Dictionary<string, string> LedColors
        {
            get => _ledColors;
            set { _ledColors = value; OnPropertyChanged(); }
        }
        public PiRunInfo CurrentRun
        {
            get => _currentRun;
            set { _currentRun = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRunDisplay)); }
        }
        public PiRunInfo NextRun
        {
            get => _nextRun;
            set { _nextRun = value; OnPropertyChanged(); OnPropertyChanged(nameof(NextRunDisplay)); }
        }
        public string CurrentRunDisplay => CurrentRun == null ? "Idle" : $"{CurrentRun.Set} {CurrentRun.Phase} ({CurrentRun.TimeRemainingSec / 60:D2}:{CurrentRun.TimeRemainingSec % 60:D2})";
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
        #endregion

        #region Constructor
        public ProgramEditorViewModel()
        {
            DebugLogger.LogVariableStatus("Constructor initialized.");
            Current = this;
            WeatherVM = new WeatherViewModel();
            _ = WeatherVM.LoadWeatherAsync();
            CalculateTodayScheduleIndex();
            LoadSchedule(); // <-- Enable real data loading
            // Debounce timer for saving and uploading schedule
            _debounceSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _debounceSaveTimer.Tick += (s, e) =>
            {
                _debounceSaveTimer.Stop();
                Save(SaveTarget.LocalAndSendToPi);
            };
            // Populate mock data for dashboard demo
            SystemStatus = "All Systems Nominal";
            // Use set names for mock ZoneStatuses if available, otherwise fallback to generic names
            var demoSetNames = new[] { "Front Lawn", "Backyard", "Garden" };
            ZoneStatuses = new ObservableCollection<ZoneStatus>
            {
                new ZoneStatus { Name = demoSetNames[0], Status = "Idle" },
                new ZoneStatus { Name = demoSetNames[1], Status = "Watering" },
                new ZoneStatus { Name = demoSetNames[2], Status = "Idle" }
            };
            LastRunDisplay = $"{DateTime.Now.AddHours(-2):yyyy-MM-dd HH:mm} - {demoSetNames[0]} (10 min)";
            UpcomingRuns = new ObservableCollection<ScheduledRunPreview>
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
            // Call UpdateNextRunFromJson to populate NextRun from JSON
            UpdateNextRunFromJson();
            var piScheduleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            piScheduleTimer.Tick += async (s, e) => await UpdatePiScheduleInfoAsync();
            piScheduleTimer.Start();
            Application.Current.Dispatcher.InvokeAsync(async () => { await UpdatePiScheduleInfoAsync(); });
            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(_ => RemoveSet());
            AddStartTimeCommand = new RelayCommand(_ => AddStartTime());
            RemoveStartTimeCommand = new RelayCommand(_ => RemoveStartTime());
            SaveScheduleCommand = new RelayCommand(_ => Save(SaveTarget.LocalOnly));
            SaveAndSendCommand = new RelayCommand(_ => Save(SaveTarget.LocalAndSendToPi));
            RunOnceCommand = new RelayCommand(_ => RunOnce());
            CopyProject = new RelayCommand(_ => CopyProject2Clip());
            RefreshPiStatusCommand = new RelayCommand(async _ => await LoadPiStatusAsync());
            ShowPiLogCommand = new RelayCommand(_ => ShowPiLog());
            ShowPlotsCommand = new RelayCommand(_ => OpenPlotWindow());
            ToggleTestModeCommand = new RelayCommand(_ => ToggleTestMode());
            StopAllCommand = new RelayCommand(async _ => await StopAllAsync());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            QuickMistCommand = new RelayCommand(_ =>
            {
                var mistSet = Sets.FirstOrDefault(s => s.SetName.Equals("Misters", StringComparison.OrdinalIgnoreCase));
                if (mistSet != null)
                {
                    SendManualRun(mistSet.SetName, 2);
                }
                else
                {
                    MessageBox.Show("Mist set not found in program.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += async (s, e) => await LoadPiStatusAsync();
            timer.Start();

            //update CurrentTime every second
            var timeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            OpenTimePickerCommand = new RelayCommand<StartTimeViewModel>(entry =>
            {
                if (entry == null)
                {
                    DebugLogger.LogVariableStatus("Entry is null. Cannot open time picker.");
                    return;
                }
                var dialog = new RadialTimePickerDialog { Owner = Application.Current.MainWindow };
                dialog.SetTime(entry.ParsedTime);
                if (dialog.ShowDialog() == true)
                {
                    entry.ParsedTime = dialog.SelectedTime;
                    DebouncedSaveAndSendToPi();
                }
            });
            SelectSectionCommand = new RelayCommand(param =>
            {
                if (param is string section)
                    SelectedSection = section;
            });

            if (Schedule.Mist == null)
                Schedule.Mist = new MistSettings();
            if (Schedule.Mist.TemperatureSettings == null || Schedule.Mist.TemperatureSettings.Count == 0)
            {
                Schedule.Mist.TemperatureSettings = new System.Collections.ObjectModel.ObservableCollection<MistSettingViewModel>
                {
                    new MistSettingViewModel { Temperature = 90, Interval = 20, Duration = 2 },
                    new MistSettingViewModel { Temperature = 95, Interval = 20, Duration = 2 },
                    new MistSettingViewModel { Temperature = 100, Interval = 20, Duration = 2 }
                };
            }
        }
        #endregion

        #region Schedule Management
        private void CalculateTodayScheduleIndex()
        {
            var baseDate = new DateTime(2024, 1, 1);
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
                    loaded.Mist = new MistSettings();
                }
                if (loaded.Mist.TemperatureSettings == null || loaded.Mist.TemperatureSettings.Count == 0)
                {
                    DebugLogger.LogFileIO("TemperatureSettings missing — initializing defaults.");
                    loaded.Mist.TemperatureSettings = new ObservableCollection<MistSettingViewModel>
                    {
                        new MistSettingViewModel { Temperature = 90, Interval = 20, Duration = 2 },
                        new MistSettingViewModel { Temperature = 95, Interval = 20, Duration = 2 },
                        new MistSettingViewModel { Temperature = 100, Interval = 20, Duration = 2 }
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
                OnPropertyChanged(nameof(Sets));
                OnPropertyChanged(nameof(StartTimes));
                OnPropertyChanged(nameof(SeasonalAdjustment));
                OnPropertyChanged(nameof(SeasonalAdjustmentPercent));
                OnPropertyChanged(nameof(VisibleSets));
                OnPropertyChanged(nameof(Week1Sunday));
                OnPropertyChanged(nameof(Week1Monday));
                OnPropertyChanged(nameof(Week1Tuesday));
                OnPropertyChanged(nameof(Week1Wednesday));
                OnPropertyChanged(nameof(Week1Thursday));
                OnPropertyChanged(nameof(Week1Friday));
                OnPropertyChanged(nameof(Week1Saturday));
                OnPropertyChanged(nameof(Week2Sunday));
                OnPropertyChanged(nameof(Week2Monday));
                OnPropertyChanged(nameof(Week2Tuesday));
                OnPropertyChanged(nameof(Week2Wednesday));
                OnPropertyChanged(nameof(Week2Thursday));
                OnPropertyChanged(nameof(Week2Friday));
                OnPropertyChanged(nameof(Week2Saturday));
                OnPropertyChanged(nameof(MistSettingsCollection));
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
        private void DebouncedSaveAndSendToPi()
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
            UpcomingRuns.Clear();
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
                        UpcomingRuns.Add(new ScheduledRunPreview
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
            var sorted = UpcomingRuns.OrderBy(r => TimeSpan.Parse(r.StartTime)).ToList();
            UpcomingRuns.Clear();
            foreach (var r in sorted)
                UpcomingRuns.Add(r);
        }
        private void MarkDirty()
        {
            DebugLogger.LogAutoSave("MarkDirty called.");
            _isDirty = true;
        }
        #endregion

        #region Pi Management
        private async Task LoadPiStatusAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://100.116.147.6:5000/status");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogVariableStatus("RAW JSON from PiStatus:", json);
                var parsed = JsonSerializer.Deserialize<PiStatusResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null)
                {
                    SystemStatus = parsed.SystemStatus;
                    PiReportedTestMode = parsed.TestMode;
                    LedColors = parsed.LedColors ?? new Dictionary<string, string>();
                    if (parsed.Zones != null && parsed.Zones.Count > 0)
                        App.Current.Dispatcher.Invoke(() => { ZoneStatuses = new ObservableCollection<ZoneStatus>(parsed.Zones); });
                    CurrentRun = parsed.CurrentRun;
                    NextRun = parsed.NextRun;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Pi status: {ex.Message}");
                App.Current.Dispatcher.Invoke(() => { CurrentRun = null; });
            }
            //update PiScheduleIndex
            OnPropertyChanged(nameof(PiScheduleIndex));
            OnPropertyChanged(nameof(PiLocalTime));
            OnPropertyChanged(nameof(PiTimezone));
            OnPropertyChanged(nameof(ScheduleIndexMismatch));
        }

        /// <summary>
        /// Current time string, binds to PiStatusViewModel.
        /// </summary>
        /// <returns></returns>
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task StopAllAsync()
        {
            try
            {
                using var client = new HttpClient();
                var result = await client.PostAsync("http://100.116.147.6:5000/stop-all", null);
                if (result.IsSuccessStatusCode)
                    MessageBox.Show("All zones stopped.", "Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Failed to stop. Check connection.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Stop Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SetEnvironmentVariable(string variableName, string value)
        {
            DebugLogger.LogVariableStatus($"SetEnvironmentVariable called: {variableName}={value}");
            if (variableName == "TEST_MODE")
            {
                try
                {
                    using var client = new HttpClient();
                    var url = "http://100.116.147.6:5000/set-test-mode";
                    var payload = new { test_mode = value == "1" };
                    DebugLogger.LogVariableStatus($"Sending to Pi: test_mode={(value == "1")}" );
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        DebugLogger.LogVariableStatus($"TEST_MODE updated via API to {value}");
                    }
                    else
                    {
                        DebugLogger.LogVariableStatus($"Failed to update TEST_MODE via API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogVariableStatus($"Failed to update TEST_MODE via API: {ex.Message}");
                }
            }
        }
        private async Task UpdatePiScheduleInfoAsync()
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync("http://100.116.147.6:5000/schedule-index");
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                PiScheduleIndex = root.GetProperty("schedule_index").GetInt32();
                PiLocalTime = root.TryGetProperty("local_time", out var timeProp) ? timeProp.GetString() : "Unavailable";
                PiTimezone = root.TryGetProperty("timezone", out var zoneProp) ? zoneProp.GetString() : "Unavailable";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to fetch Pi schedule index: {ex.Message}");
                PiScheduleIndex = null;
                PiLocalTime = "Unavailable";
                PiTimezone = "Unavailable";
            }
            //update properties
            OnPropertyChanged(nameof(PiScheduleIndex));
            OnPropertyChanged(nameof(PiLocalTime));
            OnPropertyChanged(nameof(PiTimezone));
            OnPropertyChanged(nameof(ScheduleIndexMismatch));
        }
        public async Task LoadLastWeekHistoryAsync()
        {
            // TODO: Implement logic to fetch and parse last week history from Pi (e.g., HTTP or SFTP)
            // For now, add mock data:
            LastWeekHistory.Clear();
            for (int i = 0; i < 7; i++)
            {
                LastWeekHistory.Add(new WateringLogEntry
                {
                    Date = DateTime.Today.AddDays(-i),
                    SetName = $"Zone {i + 1}",
                    DurationMinutes = 10 + i,
                    Status = "Completed"
                });
            }
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
            if (CurrentRun != null && CurrentRun.Phase != null)
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/running.png";
            }
            else if (status.Contains("Offline"))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/offline.png";
            }
            else if (status.Contains("Loading"))
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/loading.png";
            }
            else
            {
                StatusIconPath = "pack://application:,,,/Assets/Icons/unknown.png";
            }
        }
        private void ToggleTestMode()
        {
            bool newValue = !PiReportedTestMode;
            DebugLogger.LogVariableStatus($"ToggleTestMode called. PiReportedTestMode={PiReportedTestMode}, sending newValue={newValue}");
            SetEnvironmentVariable("TEST_MODE", newValue ? "1" : "0");
            // Do not set PiReportedTestMode here; rely on Pi status polling
        }
        private void OpenPlotWindow()
        {
            var window = new WateringHistoryView();
            window.Show();
        }
        private async void ShowPiLog()
        {
            await LoadPiStatusAsync();
            if (PiStatusLog.Count == 0)
            {
                MessageBox.Show("Log is empty.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new PiLogWindow(PiStatusLog);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            DebugLogger.LogPropertyChange($"PropertyChanged: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Debug
        // DebugLog method replaced by DebugLogger usage throughout the class.
        #endregion

        #region Nested Types
        #endregion

        #region New Methods
        public void UpdateNextRunFromJson()
        {
            try
            {
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sprinkler_schedule.json");
                if (!File.Exists(jsonPath)) return;
                var json = File.ReadAllText(jsonPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var startTimes = root.GetProperty("start_times").EnumerateArray()
                    .Where(st => st.GetProperty("isEnabled").GetBoolean())
                    .Select(st => TimeSpan.Parse(st.GetProperty("time").GetString()))
                    .OrderBy(t => t)
                    .ToList();
                if (startTimes.Count == 0) return;
                var now = DateTime.Now.TimeOfDay;
                var nextTime = startTimes.FirstOrDefault(t => t > now);
                if (nextTime == default) nextTime = startTimes.First();
                var sets = root.GetProperty("sets").EnumerateArray()
                    .Where(s => s.TryGetProperty("run_duration_minutes", out _))
                    .Select(s => new
                    {
                        Name = s.GetProperty("set_name").GetString(),
                        Duration = s.GetProperty("run_duration_minutes").GetInt32()
                    })
                    .ToList();
                if (sets.Count == 0) return;
                var nextSet = sets.First();
                NextRun = new PiRunInfo {
                    Set = nextSet.Name,
                    StartTime = nextTime.ToString(@"hh\:mm"),
                    DurationMinutes = nextSet.Duration
                };
            }
            catch { /* ignore errors for now */ }
        }
        #endregion
    }
}
