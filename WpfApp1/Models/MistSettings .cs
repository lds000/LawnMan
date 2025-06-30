using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class MistSettings : INotifyPropertyChanged
    {
        [JsonPropertyName("temperature_settings")]
        public ObservableCollection<MistSettingViewModel> TemperatureSettings { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MistSettingViewModel : INotifyPropertyChanged
    {
        private int _interval;
        private double _duration; // Changed from int to double
        private bool _mistEnabled = true; // Default to true

        [JsonPropertyName("temperature")]
        public int Temperature { get; set; }

        [JsonPropertyName("interval")]
        public int Interval
        {
            get => _interval;
            set { if (_interval != value) { _interval = value; OnPropertyChanged();
                if (BackyardBoss.ViewModels.ProgramEditorViewModel.Current != null)
                    BackyardBoss.ViewModels.ProgramEditorViewModel.Current.DebouncedSaveAndSendToPi();
            } }
        }

        [JsonPropertyName("duration")]
        public double Duration // Changed from int to double
        {
            get => _duration;
            set { if (_duration != value) { _duration = value; OnPropertyChanged();
                if (BackyardBoss.ViewModels.ProgramEditorViewModel.Current != null)
                    BackyardBoss.ViewModels.ProgramEditorViewModel.Current.DebouncedSaveAndSendToPi();
            } }
        }

        [JsonPropertyName("mist_enabled")]
        [JsonInclude]
        public bool MistEnabled
        {
            get => _mistEnabled;
            set { if (_mistEnabled != value) { _mistEnabled = value; OnPropertyChanged();
                if (BackyardBoss.ViewModels.ProgramEditorViewModel.Current != null)
                    BackyardBoss.ViewModels.ProgramEditorViewModel.Current.DebouncedSaveAndSendToPi();
            } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
