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
using System.Collections.Specialized;
using BackyardBoss.Views;

namespace BackyardBoss.ViewModels
{
    public class ProgramEditorViewModel : INotifyPropertyChanged
    {
        private WateringProgram _selectedProgram;
        private ProgramSet _selectedSet;
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


            OpenTimePickerCommand = new RelayCommand<StartTimeViewModel>(entry =>
            {
                var dialog = new RadialTimePickerDialog
                {
                    Owner = Application.Current.MainWindow
                };

                // Pre-load the time into the dialog
                dialog.SetTime(entry.ParsedTime);

                // Show the dialog modally
                if (dialog.ShowDialog() == true)
                {
                    // Update the selected time after user confirms
                    entry.ParsedTime = dialog.SelectedTime;
                }
            });

        }

        public StartTimeViewModel StartTime1 { get; set; } = new StartTimeViewModel { Time = "06:00" };



        public SprinklerSchedule Schedule { get; private set; } = new SprinklerSchedule();
        public ObservableCollection<WateringProgram> Programs => Schedule.Programs;
        public ObservableCollection<ScheduledRunPreview> UpcomingRuns { get; private set; } = new ObservableCollection<ScheduledRunPreview>();
        public ObservableCollection<StartTimeViewModel> StartTimes { get; set; } = new ObservableCollection<StartTimeViewModel>();

        public bool HasUnsavedChanges => _isDirty;

        public WateringProgram SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProgramSelected));

                if (_selectedProgram != null)
                {
                    StartTimes.Clear();
                    foreach (var t in _selectedProgram.StartTimes)
                        StartTimes.Add(new StartTimeViewModel { Time = t });

                    foreach (var vm in StartTimes)
                    {
                        vm.PropertyChanged += (_, e) =>
                        {
                            if (e.PropertyName == nameof(StartTimeViewModel.Time))
                                UpdateStartTimeCollectionFromViewModels();
                        };
                    }
                }
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

        private async void LoadSchedule()
        {
            Schedule = await ProgramDataService.LoadScheduleAsync();
            _isDirty = false;
            OnPropertyChanged(nameof(Programs));

            foreach (var program in Programs)
            {
                program.PropertyChanged += Program_PropertyChanged;
                program.StartTimes.CollectionChanged += (s, e) =>
                {
                    MarkDirty();
                    UpdateUpcomingRunsPreview();
                };
            }

            SelectedProgram = Programs.FirstOrDefault();
            UpdateUpcomingRunsPreview();
        }

        private async void SaveSchedule()
        {
            UpdateStartTimeCollectionFromViewModels();
            await ProgramDataService.SaveScheduleAsync(Schedule);
            _isDirty = false;
            MessageBox.Show("Schedule saved successfully!", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateUpcomingRunsPreview();
        }

        private void AddProgram()
        {
            var newProgram = new WateringProgram { Name = "New Program" };
            newProgram.PropertyChanged += Program_PropertyChanged;
            newProgram.StartTimes.CollectionChanged += (s, e) => { MarkDirty(); UpdateUpcomingRunsPreview(); };
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
                SelectedProgram.Sets.Add(new ProgramSet { SetName = "New Set", RunDurationMinutes = 10 });
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
            StartTimes.Add(new StartTimeViewModel { Time = "06:00" });
            MarkDirty();
        }

        private void RemoveStartTime()
        {
            if (StartTimes.Count > 0)
            {
                StartTimes.RemoveAt(StartTimes.Count - 1);
                MarkDirty();
            }
        }

        private void UpdateStartTimeCollectionFromViewModels()
        {
            if (SelectedProgram != null)
            {
                SelectedProgram.StartTimes.Clear();
                foreach (var vm in StartTimes)
                    SelectedProgram.StartTimes.Add(vm.Time);

                SortStartTimes();
                MarkDirty();
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void Program_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WateringProgram.IsActive))
            {
                MarkDirty();
                UpdateUpcomingRunsPreview();
            }
        }
    }
}
