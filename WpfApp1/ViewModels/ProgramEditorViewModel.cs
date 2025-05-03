// ProgramEditorViewModel with verbose auto-save logging
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BackyardBoss.Commands;
using BackyardBoss.Models;
using BackyardBoss.Services;
using BackyardBoss.Views;

namespace BackyardBoss.ViewModels
{
    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        private SprinklerSet _selectedSet;
        private string _selectedStartTime;
        private bool _isDirty;

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

        public static ProgramEditorViewModel Current
        {
            get; private set;
        }

        public ProgramEditorViewModel()
        {
            DebugLog("Constructor initialized.");
            Current = this;
            LoadSchedule();

            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(_ => RemoveSet());
            AddStartTimeCommand = new RelayCommand(_ => AddStartTime());
            RemoveStartTimeCommand = new RelayCommand(_ => RemoveStartTime());
            SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());

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

        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();
        public ObservableCollection<SprinklerSet> Sets => Schedule.Sets;
        public ObservableCollection<StartTimeViewModel> StartTimes { get; private set; } = new();
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

                StartTimes.Clear();
                foreach (var time in loaded.StartTimes)
                {
                    DebugLog($"Loaded start time: {time}");
                    StartTimes.Add(new StartTimeViewModel { Time = time });
                }

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
            Schedule.StartTimes = new ObservableCollection<string>(StartTimes.Select(t => t.Time));
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

        private void AutoSave()
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
