// ProgramEditorViewModel with verbose auto-save logging
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
using BackyardBoss.ViewModels; // or wherever your WeatherViewModel class is



namespace BackyardBoss.ViewModels
{
    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        private SprinklerSet _selectedSet;
        private string _selectedStartTime;
        private bool _isDirty;
        public ObservableCollection<StartTimeViewModel> StartTimes => Schedule.StartTimes;

        public ICommand OpenTimePickerCommand
        {
            get;
        }
        public ICommand SaveScheduleCommand
        {
            get;
        }
        public ICommand AddSetCommand
        {
            get;
        }
        public ICommand RemoveSetCommand
        {
            get;
        }
        public ICommand AddStartTimeCommand
        {
            get;
        }
        public ICommand RemoveStartTimeCommand
        {
            get;
        }

        public ICommand RunOnceCommand
        {
            get;
        }

 
        public ICommand SaveAndSendCommand
        {
            get;
        }

        public ICommand CopyProject
        {
            get;
        }

        public ICommand QuickMistCommand
        {
            get;
        }

        public ICommand RefreshPiStatusCommand
        {
            get;
        }

        public ICommand ShowPiLogCommand
        {
            get;
        }

        public ICommand ShowPlotsCommand
        {
            get;
        }

        public WeatherViewModel WeatherVM { get; } = new WeatherViewModel();

        private int _todayScheduleIndex = -1;
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

        private void CalculateTodayScheduleIndex()
        {
            var baseDate = new DateTime(2024, 1, 1); // Must match Pi logic
            var today = DateTime.Today;
            var deltaDays = (today - baseDate).Days;
            TodayScheduleIndex = deltaDays % 14;
        }

        public bool IsTodayIndex(int index) => index == TodayScheduleIndex;

        private string _statusIconPath;
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

        private string _currentRunStatus = "Unknown"; // Default status
        public string CurrentRunStatus
        {
            get => _currentRunStatus;
            set
            {
                _currentRunStatus = value;
                OnPropertyChanged();
                UpdateStatusIcon(value); // 👈 new method
            }
        }

        private void UpdateStatusIcon(string status)
        {
            if (status.Contains("Idle"))
                StatusIconPath = "pack://application:,,,/Assets/Icons/idle.png";
            else if (status.Contains("("))
                StatusIconPath = "pack://application:,,,/Assets/Icons/running.png";
            else if (status.Contains("Offline"))
                StatusIconPath = "pack://application:,,,/Assets/Icons/offline.png";
            else if (status.Contains("Loading"))
                StatusIconPath = "pack://application:,,,/Assets/Icons/loading.png";
            else
                StatusIconPath = "pack://application:,,,/Assets/Icons/unknown.png";
        }






        public ObservableCollection<string> PiStatusLog { get; private set; } = new();
        private async Task LoadPiStatusAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://100.116.147.6:5000/status");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("RAW JSON:");
                Debug.WriteLine(json);

                var parsed = JsonSerializer.Deserialize<PiStatusResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (parsed?.Log == null)
                {
                    Debug.WriteLine("Deserialization failed or log is null.");
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    PiStatusLog.Clear();
                    foreach (var line in parsed.Log)
                        PiStatusLog.Add(line);

                    // Update CurrentRunStatus with countdown
                    if (parsed.Current_Run?.Running == true)
                    {
                        var minutes = parsed.Current_Run.Time_Remaining_Sec / 60;
                        var seconds = parsed.Current_Run.Time_Remaining_Sec % 60;
                        CurrentRunStatus = $"{parsed.Current_Run.Set}\n({minutes:D2}:{seconds:D2})";
                    }
                    else
                    {
                        CurrentRunStatus = "Idle";
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Pi status: {ex.Message}");
                App.Current.Dispatcher.Invoke(() =>
                {
                    CurrentRunStatus = "Offline";
                });
            }

        }



        // DTO to match Flask response
        private class PiStatusResponse
        {
            public List<string> Log { get; set; } = new();
            public CurrentRunInfo Current_Run { get; set; } = new();
        }

        public class CurrentRunInfo
        {
            public bool Running
            {
                get; set;
            }
            public string Set
            {
                get; set;
            }
            public int Time_Remaining_Sec
            {
                get; set;
            }
        }



        private void SaveAndSendSchedule()
        {
            string localPath = "sprinkler_schedule.json";
            string remotePath = "/home/lds00/sprinkler_schedule.json";
            string piHost = "100.116.147.6";         // Replace with actual IP
            string username = "lds00";
            string password = "Celica1!";

            try
            {
                Schedule.StartTimes = new ObservableCollection<StartTimeViewModel>(StartTimes);


                var json = JsonSerializer.Serialize(Schedule, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });


                File.WriteAllText(localPath, json);

                using var sftp = new SftpClient(piHost, username, password);
                sftp.Connect();
                using var stream = File.OpenRead(localPath);
                sftp.UploadFile(stream, remotePath, true);
                sftp.Disconnect();

                MessageBox.Show("Schedule saved and sent to Raspberry Pi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save and send: {ex.Message}");
            }
        }

        public static List<string> Modes => new() { "scheduled", "manual", "disabled" };


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

                DebugLog("RunOnce: Executing the following sets:");
                foreach (var run in selected)
                {
                    DebugLog($" - {run.SetName}: {run.Duration} min");
                }

                // 👇 Prepare manual_command.json
                var command = new
                {
                    manual_run = new
                    {
                        sets = selected.Select(s => s.SetName).ToArray(),
                        duration_minutes = selected.Max(s => s.Duration) // You may want a smarter handling here
                    }
                };

                string localPath = "manual_command.json";
                string remotePath = "/home/lds00/manual_command.json";
                string piHost = "100.116.147.6";  // ← Replace with actual IP
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
            string remotePath = "/home/lds00/manual_command.json";
            string piHost = "100.116.147.6";  // ← Replace with actual IP
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


        public static ProgramEditorViewModel Current
        {
            get; private set;
        }
        
        public ObservableCollection<SprinklerSet> VisibleSets =>
    new ObservableCollection<SprinklerSet>(
        Sets.Where(s => !s.SetName.Equals("Misters", StringComparison.OrdinalIgnoreCase)));
        public ProgramEditorViewModel()
        {
            DebugLog("Constructor initialized.");
            Current = this;

            WeatherVM = new WeatherViewModel();
            _ = WeatherVM.LoadWeatherAsync(); // ✅ Start weather loading

            CurrentRunStatus = "Loading"; // Default status

            CalculateTodayScheduleIndex();

            LoadSchedule();

            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(_ => RemoveSet());
            AddStartTimeCommand = new RelayCommand(_ => AddStartTime());
            RemoveStartTimeCommand = new RelayCommand(_ => RemoveStartTime());
            SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());
            RunOnceCommand = new RelayCommand(_ => RunOnce());
            SaveAndSendCommand = new RelayCommand(_ => SaveAndSendSchedule());
            CopyProject = new RelayCommand(_ => CopyProject2Clip());
            RefreshPiStatusCommand = new RelayCommand(async _ => await LoadPiStatusAsync());
            ShowPiLogCommand = new RelayCommand(_ => ShowPiLog());
            ShowPlotsCommand = new RelayCommand(_ => OpenPlotWindow());


            QuickMistCommand = new RelayCommand(_ =>
            {
                MistDuration = 2;
                AutoSave();

                var mistSet = Sets.FirstOrDefault(s => s.SetName.Equals("Misters", StringComparison.OrdinalIgnoreCase));
                if (mistSet != null)
                {
                    SendManualRun(mistSet.SetName, MistDuration);
                }
                else
                {
                    MessageBox.Show("Mist set not found in program.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += async (s, e) => await LoadPiStatusAsync();
            timer.Start();

            OpenTimePickerCommand = new RelayCommand<StartTimeViewModel>(entry =>
            {
                var dialog = new RadialTimePickerDialog { Owner = Application.Current.MainWindow };
                dialog.SetTime(entry.ParsedTime);
                if (dialog.ShowDialog() == true)
                {
                    entry.ParsedTime = dialog.SelectedTime;
                    AutoSave();
                }
            });
        }

        private void OpenPlotWindow()
        {
            var window = new WateringHistoryView();
            window.Show();
        }



        private async void ShowPiLog()
        {
            await LoadPiStatusAsync(); // ensure data is fetched

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
            //string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //string pointing to directory of my solution
            string rootDirectory = @"C:\Users\lds00\source\repos\WpfApp1\WpfApp1\";


            BackyardBoss.Tools.ProjectStructureToClipboard.ExportStructureWithContents(rootDirectory);
        }

        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();
        public ObservableCollection<SprinklerSet> Sets => Schedule.Sets;
        public ObservableCollection<ScheduledRunPreview> UpcomingRuns { get; private set; } = new();
        public bool HasUnsavedChanges => _isDirty;

        public SprinklerSet SelectedSet
        {
            get => _selectedSet;
            set
            {
                _selectedSet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSetSelected));
            }
        }

        public int MistDuration
        {
            get => Schedule.Mist.DurationMinutes;
            set
            {
                if (Schedule.Mist.DurationMinutes != value)
                {
                    Schedule.Mist.DurationMinutes = value;
                    OnPropertyChanged();
                    AutoSave();
                }
            }
        }

        public bool MistTime1030
        {
            get => Schedule.Mist.Time1030;
            set
            {
                if (Schedule.Mist.Time1030 != value)
                {
                    Schedule.Mist.Time1030 = value;
                    OnPropertyChanged();
                    AutoSave();
                }
            }
        }

        public bool MistTime1330
        {
            get => Schedule.Mist.Time1330;
            set
            {
                if (Schedule.Mist.Time1330 != value)
                {
                    Schedule.Mist.Time1330 = value;
                    OnPropertyChanged();
                    AutoSave();
                }
            }
        }

        public bool MistTime1600
        {
            get => Schedule.Mist.Time1600;
            set
            {
                if (Schedule.Mist.Time1600 != value)
                {
                    Schedule.Mist.Time1600 = value;
                    OnPropertyChanged();
                    AutoSave();
                }
            }
        }

        public int? MistPulseDuration
        {
            get => Schedule.Mist.PulseDurationMinutes;
            set
            {
                if (Schedule.Mist.PulseDurationMinutes != value)
                {
                    Schedule.Mist.PulseDurationMinutes = value;
                    OnPropertyChanged();
                    AutoSave();
                }
            }
        }

        public int? MistSoakDuration
        {
            get => Schedule.Mist.SoakDurationMinutes;
            set
            {
                if (Schedule.Mist.SoakDurationMinutes != value)
                {
                    Schedule.Mist.SoakDurationMinutes = value;
                    OnPropertyChanged();
                    AutoSave();
                }
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

                    // Notify all sets their adjusted minutes may have changed
                    foreach (var set in Sets)
                    {
                        set.OnPropertyChanged(nameof(set.SeasonallyAdjustedMinutes));
                    }

                    UpdateUpcomingRunsPreview();
                    AutoSave();
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

        // Weekday properties omitted for brevity but should call AutoSave in setters
        // Week 1
        public bool Week1Sunday
        {
            get => Schedule.ScheduleDays[0]; set
            {
                Schedule.ScheduleDays[0] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Monday
        {
            get => Schedule.ScheduleDays[1]; set
            {
                Schedule.ScheduleDays[1] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Tuesday
        {
            get => Schedule.ScheduleDays[2]; set
            {
                Schedule.ScheduleDays[2] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Wednesday
        {
            get => Schedule.ScheduleDays[3]; set
            {
                Schedule.ScheduleDays[3] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Thursday
        {
            get => Schedule.ScheduleDays[4]; set
            {
                Schedule.ScheduleDays[4] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Friday
        {
            get => Schedule.ScheduleDays[5]; set
            {
                Schedule.ScheduleDays[5] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week1Saturday
        {
            get => Schedule.ScheduleDays[6]; set
            {
                Schedule.ScheduleDays[6] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }

        // Week 2
        public bool Week2Sunday
        {
            get => Schedule.ScheduleDays[7]; set
            {
                Schedule.ScheduleDays[7] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Monday
        {
            get => Schedule.ScheduleDays[8]; set
            {
                Schedule.ScheduleDays[8] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Tuesday
        {
            get => Schedule.ScheduleDays[9]; set
            {
                Schedule.ScheduleDays[9] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Wednesday
        {
            get => Schedule.ScheduleDays[10]; set
            {
                Schedule.ScheduleDays[10] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Thursday
        {
            get => Schedule.ScheduleDays[11]; set
            {
                Schedule.ScheduleDays[11] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Friday
        {
            get => Schedule.ScheduleDays[12]; set
            {
                Schedule.ScheduleDays[12] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
        public bool Week2Saturday
        {
            get => Schedule.ScheduleDays[13]; set
            {
                Schedule.ScheduleDays[13] = value;
                OnPropertyChanged();
                AutoSave();
            }
        }

        private string _currentStation;
        public string CurrentStation
        {
            get => _currentStation;
            set
            {
                if (_currentStation != value)
                {
                    _currentStation = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _countdown;
        public string Countdown
        {
            get => _countdown;
            set
            {
                if (_countdown != value)
                {
                    _countdown = value;
                    OnPropertyChanged();
                }
            }
        }



        public bool IsSetSelected => SelectedSet != null;
        public bool IsStartTimeSelected => !string.IsNullOrEmpty(SelectedStartTime);

        private async void LoadSchedule()
        {
            DebugLog("Loading schedule...");
            var loaded = await ProgramDataService.LoadScheduleAsync();
            if (loaded != null)
            {
                Schedule = loaded;

                if (loaded.ScheduleDays == null || loaded.ScheduleDays.Count != 14)
                {
                    DebugLog("Initializing blank ScheduleDays array.");
                    loaded.ScheduleDays = new ObservableCollection<bool>(Enumerable.Repeat(false, 14));
                }

                if (loaded.Mist == null)
                {
                    DebugLog("Mist section missing — initializing defaults.");
                    loaded.Mist = new MistSettings();
                }

                if (loaded.StartTimes == null || loaded.StartTimes.Count == 0)
                {
                    DebugLog("No start times found — inserting default 06:00 and 17:00.");
                    loaded.StartTimes = new ObservableCollection<StartTimeViewModel>
    {
        new StartTimeViewModel { Time = "06:00", IsEnabled = true },
        new StartTimeViewModel { Time = "17:00", IsEnabled = true }
    };
                }



                OnPropertyChanged(nameof(StartTimes));
                OnPropertyChanged(nameof(VisibleSets));



                OnPropertyChanged(nameof(Sets));
                OnPropertyChanged(nameof(StartTimes));
                OnPropertyChanged(nameof(SeasonalAdjustment));
                OnPropertyChanged(nameof(SeasonalAdjustmentPercent));

                // Notify WPF of each schedule day toggle
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

                OnPropertyChanged(nameof(SeasonalAdjustment));
                OnPropertyChanged(nameof(SeasonalAdjustmentPercent));

                OnPropertyChanged(nameof(MistDuration));
                OnPropertyChanged(nameof(MistTime1030));
                OnPropertyChanged(nameof(MistTime1330));
                OnPropertyChanged(nameof(MistTime1600));

                _isDirty = false;
                DebugLog("Schedule loaded successfully.");
                UpdateUpcomingRunsPreview();
            }
            else
            {
                DebugLog("Failed to load schedule.");
            }
        }


        private async void SaveSchedule()
        {
            DebugLog("SaveSchedule triggered.");

            // No conversion needed; StartTimes is already ObservableCollection<StartTimeViewModel>
            Schedule.StartTimes = new ObservableCollection<StartTimeViewModel>(StartTimes);

            DebugLog($"Preparing to save {Schedule.Sets.Count} sets and {Schedule.StartTimes.Count} start times.");

            try
            {
                await ProgramDataService.SaveScheduleAsync(Schedule);
                _isDirty = false;
                DebugLog("Save successful.");
            }
            catch (Exception ex)
            {
                DebugLog($"Save failed: {ex.Message}");
            }
        }


        public void AutoSave()
        {
            DebugLog("AutoSave called.");
            MarkDirty();
            SaveSchedule();
        }

        private void AddSet()
        {
            var newSet = new SprinklerSet { SetName = "New Set", RunDurationMinutes = 10 };
            Sets.Add(newSet);
            DebugLog($"Set added: {newSet.SetName}, {newSet.RunDurationMinutes} min.");
            AutoSave();
            UpdateUpcomingRunsPreview();
        }

        private void RemoveSet()
        {
            if (SelectedSet != null)
            {
                DebugLog($"Removing set: {SelectedSet.SetName}");
                Sets.Remove(SelectedSet);
                SelectedSet = null;
                AutoSave();
                UpdateUpcomingRunsPreview();
            }
            else
            {
                DebugLog("RemoveSet called, but no set was selected.");
            }
        }

        private void AddStartTime()
        {
            var time = new StartTimeViewModel { Time = "06:00" };
            StartTimes.Add(time);
            DebugLog($"Start time added: {time.Time}");
            AutoSave();
            UpdateUpcomingRunsPreview();
        }

        private void RemoveStartTime()
        {
            if (StartTimes.Count > 0)
            {
                var removed = StartTimes.Last();
                StartTimes.Remove(removed);
                DebugLog($"Start time removed: {removed.Time}");
                AutoSave();
                UpdateUpcomingRunsPreview();
            }
            else
            {
                DebugLog("RemoveStartTime called but list was empty.");
            }
        }

        public void SortStartTimes()
        {
            DebugLog("Sorting start times.");
            var sorted = StartTimes.OrderBy(t => TimeSpan.Parse(t.Time)).ToList();
            StartTimes.Clear();
            foreach (var t in sorted)
            {
                DebugLog($" - {t.Time}");
                StartTimes.Add(t);
            }
            AutoSave();
            UpdateUpcomingRunsPreview();
        }

        public void UpdateUpcomingRunsPreview()
        {
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
            DebugLog("MarkDirty called.");
            _isDirty = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            DebugLog($"PropertyChanged: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DebugLog(string message, [CallerMemberName] string caller = "")
        {
            if (Properties.Settings.Default.DEBUG_VERBOSE)
            {
                Debug.WriteLine($"[DEBUG {DateTime.Now:HH:mm:ss.fff}] {caller}: {message}");
            }
        }
    }
}
