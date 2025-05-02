using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BackyardBoss.Models
{
    public class WateringProgram : INotifyPropertyChanged
    {
        [JsonProperty("name")]
        public string Name
        {
            get; set;
        }

        [JsonProperty("start_times")]
        public ObservableCollection<string> StartTimes { get; set; } = new ObservableCollection<string>();

        [JsonProperty("sets")]
        public ObservableCollection<ProgramSet> Sets { get; set; } = new ObservableCollection<ProgramSet>();

        private bool _isActive = true;
        [JsonProperty("is_active")]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
