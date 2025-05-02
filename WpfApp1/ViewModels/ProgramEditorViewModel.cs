using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BackyardBoss.Models;
using BackyardBoss.Commands;
using BackyardBoss.Services;

namespace BackyardBoss.ViewModels
{
    /// <summary>
    /// ViewModel for editing sprinkler programs, including sets and start times.
    /// Handles UI interaction and command logic.
    /// </summary>
    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        // Private fields for selection and dirty tracking
        private WateringProgram _selectedProgram;
        private ProgramSet _selectedSet;
        private string _selectedStartTime;
        private bool _isDirty;

        // Constructor: Initializes the ViewModel and sets up commands
        public ProgramEditorViewModel()
        {
            LoadSchedule();

            AddProgramCommand = new RelayCommand(AddProgram);
            RemoveProgramCommand = new RelayCommand(RemoveProgram);
            AddSetCommand = new RelayCommand(AddSet);
            RemoveSetCommand = new RelayCommand(RemoveSet);
            AddStartTimeCommand = new RelayCommand(AddStartTime);
            RemoveStartTimeCommand = new RelayCommand(RemoveStartTime);
            SaveScheduleCommand = new RelayCommand(SaveSchedule);
        }

        #region Public Properties

        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();

        public ObservableCollection<WateringProgram> Programs => Schedule.Programs;

        public ObservableCollection<ScheduledRunPreview> UpcomingRuns { get; private set; } = new ObservableCollection<ScheduledRunPreview>();

        public bool HasUnsavedChanges => _isDirty;

        public WateringProgram SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProgramSelected));
            }
        }

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

        public bool IsProgramSelected => SelectedProgram != null;
        public bool IsSetSelected => SelectedSet != null;
        public bool IsStartTimeSelected => !string.IsNullOrEmpty(SelectedStartTime);

        #endregion

        #region Commands

        public ICommand SaveScheduleCommand
        {
            get;
        }
        public ICommand AddProgramCommand
        {
            get;
        }
        public ICommand RemoveProgramCommand
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

        #region Load and Save

        private async void LoadSchedule()
        {
            Schedule = await ProgramDataService.LoadScheduleAsync();
            _isDirty = false;
            OnPropertyChanged(nameof(Programs));

            foreach (var program in Programs)
            {
                program.PropertyChanged += Program_PropertyChanged;
                program.StartTimes.CollectionChanged += (s, e) => {
                    MarkDirty();
                    UpdateUpcomingRunsPreview();
                };
            }

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

        #region Command Handlers

        private void AddProgram()
        {
            var newProgram = new WateringProgram { Name = "New Program" };
            newProgram.PropertyChanged += Program_PropertyChanged;
            newProgram.StartTimes.CollectionChanged += (s, e) => {
                MarkDirty();
                UpdateUpcomingRunsPreview();
            };

            Programs.Add(newProgram);
            SelectedProgram = newProgram;
            MarkDirty();
            UpdateUpcomingRunsPreview();
        }

        private void RemoveProgram()
        {
            if (SelectedProgram != null)
            {
                Programs.Remove(SelectedProgram);
                SelectedProgram = null;
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        private void AddSet()
        {
            if (SelectedProgram != null)
            {
                SelectedProgram.Sets.Add(new ProgramSet
                {
                    SetName = "New Set",
                    RunDurationMinutes = 10
                });
                OnPropertyChanged(nameof(SelectedProgram));
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        private void RemoveSet()
        {
            if (SelectedProgram != null && SelectedSet != null)
            {
                SelectedProgram.Sets.Remove(SelectedSet);
                SelectedSet = null;
                OnPropertyChanged(nameof(SelectedProgram));
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        private void AddStartTime()
        {
            if (SelectedProgram != null)
            {
                var window = new Views.PickTimeWindow
                {
                    Owner = Application.Current.MainWindow,
                    ViewModel = this
                };

                if (window.ShowDialog() == true && !string.IsNullOrEmpty(window.SelectedTime))
                {
                    SelectedProgram.StartTimes.Add(window.SelectedTime);
                    SortStartTimes();
                    OnPropertyChanged(nameof(SelectedProgram));
                    MarkDirty();
                    UpdateUpcomingRunsPreview();
                }
            }
        }

        private void RemoveStartTime()
        {
            if (SelectedProgram != null && SelectedProgram.StartTimes.Count > 0)
            {
                SelectedProgram.StartTimes.RemoveAt(SelectedProgram.StartTimes.Count - 1);
                OnPropertyChanged(nameof(SelectedProgram));
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        #endregion

        #region Helper Methods

        public void SortStartTimes()
        {
            if (SelectedProgram != null)
            {
                var sorted = SelectedProgram.StartTimes.OrderBy(t => TimeSpan.Parse(t)).ToList();
                SelectedProgram.StartTimes.Clear();
                foreach (var time in sorted)
                    SelectedProgram.StartTimes.Add(time);

                OnPropertyChanged(nameof(SelectedProgram));
                UpdateUpcomingRunsPreview();
            }
        }

        public void UpdateUpcomingRunsPreview()
        {
            UpcomingRuns.Clear();
            var now = DateTime.Now.TimeOfDay;

            foreach (var program in Programs.Where(p => p.IsActive))
            {
                foreach (var start in program.StartTimes)
                {
                    if (TimeSpan.TryParse(start, out var programStartTime) && programStartTime >= now)
                    {
                        var cumulativeTime = programStartTime;

                        foreach (var set in program.Sets)
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
            }

            var sorted = UpcomingRuns.OrderBy(r => TimeSpan.Parse(r.StartTime)).ToList();
            UpcomingRuns.Clear();
            foreach (var run in sorted)
                UpcomingRuns.Add(run);
        }

        public void MarkDirty() => _isDirty = true;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Event Handlers

        private void Program_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WateringProgram.IsActive))
            {
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }

        #endregion
    }
}
