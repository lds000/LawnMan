using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Collections.Specialized;
using BackyardBoss.Models;
using BackyardBoss.Commands;
using BackyardBoss.Services;
using BackyardBoss.Views;

namespace BackyardBoss.ViewModels
{
    /// <summary>
    /// ViewModel for managing a single sprinkler schedule using the MVVM pattern.
    /// Handles scheduling, editing, and start time previewing.
    /// </summary>
    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        #region Fields

        private ProgramSet _selectedSet;
        private string _selectedStartTime;
        private bool _isDirty;

        #endregion

        #region Commands

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

        #endregion

        #region Constructor

        public ProgramEditorViewModel()
        {
            LoadSchedule();

            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(_ => RemoveSet());
            AddStartTimeCommand = new RelayCommand(_ => AddStartTime());
            RemoveStartTimeCommand = new RelayCommand(_ => RemoveStartTime());
            SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());

            OpenTimePickerCommand = new RelayCommand<StartTimeViewModel>(entry =>
            {
                var dialog = new RadialTimePickerDialog
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.SetTime(entry.ParsedTime);

                if (dialog.ShowDialog() == true)
                {
                    entry.ParsedTime = dialog.SelectedTime;
                }
            });
        }

        #endregion

        #region Properties

        public StartTimeViewModel StartTime1 { get; set; } = new StartTimeViewModel { Time = "06:00" };

        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();

        public ObservableCollection<ProgramSet> Sets => Schedule.Sets;

        public ObservableCollection<string> StartTimes => Schedule.StartTimes;

        public ObservableCollection<ScheduledRunPreview> UpcomingRuns { get; private set; } = new ObservableCollection<ScheduledRunPreview>();

        public bool HasUnsavedChanges => _isDirty;

        public ProgramSet SelectedSet
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

        public bool IsSetSelected => SelectedSet != null;
        public bool IsStartTimeSelected => !string.IsNullOrEmpty(SelectedStartTime);

        #endregion

        #region Schedule Load/Save

        private async void LoadSchedule()
        {
            Schedule = await ProgramDataService.LoadScheduleAsync();
            _isDirty = false;
            UpdateUpcomingRunsPreview();
        }

        private async void SaveSchedule()
        {
            await ProgramDataService.SaveScheduleAsync(Schedule);
            _isDirty = false;
            MessageBox.Show("Schedule saved successfully!", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateUpcomingRunsPreview();
        }

        #endregion

        #region Set Management

        private void AddSet()
        {
            Sets.Add(new ProgramSet { SetName = "New Set", RunDurationMinutes = 10 });
            MarkDirty();
            UpdateUpcomingRunsPreview();
        }

        private void RemoveSet()
        {
            if (SelectedSet != null)
            {
                Sets.Remove(SelectedSet);
                SelectedSet = null;
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        #endregion

        #region Start Time Management

        private void AddStartTime()
        {
            StartTimes.Add("06:00");
            MarkDirty();
            UpdateUpcomingRunsPreview();
        }

        private void RemoveStartTime()
        {
            if (StartTimes.Count > 0)
            {
                StartTimes.RemoveAt(StartTimes.Count - 1);
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        public void SortStartTimes()
        {
            var sorted = StartTimes.OrderBy(t => TimeSpan.Parse(t)).ToList();
            StartTimes.Clear();
            foreach (var time in sorted)
                StartTimes.Add(time);

            UpdateUpcomingRunsPreview();
        }

        #endregion

        #region Preview Generation

        public void UpdateUpcomingRunsPreview()
        {
            UpcomingRuns.Clear();
            var now = DateTime.Now.TimeOfDay;

            foreach (var start in StartTimes)
            {
                if (TimeSpan.TryParse(start, out var programStartTime) && programStartTime >= now)
                {
                    var cumulativeTime = programStartTime;

                    foreach (var set in Sets)
                    {
                        UpcomingRuns.Add(new ScheduledRunPreview
                        {
                            SetName = set.SetName,
                            StartTime = cumulativeTime.ToString(@"hh\:mm"),
                            RunDurationMinutes = set.RunDurationMinutes
                        });

                        cumulativeTime = cumulativeTime.Add(TimeSpan.FromMinutes(set.RunDurationMinutes));
                    }
                }
            }

            var sorted = UpcomingRuns.OrderBy(r => TimeSpan.Parse(r.StartTime)).ToList();
            UpcomingRuns.Clear();
            foreach (var run in sorted)
                UpcomingRuns.Add(run);
        }

        #endregion

        #region Helpers

        public void MarkDirty() => _isDirty = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
