using System.ComponentModel;
using BackyardBoss.Models;

namespace BackyardBoss.ViewModels
{
    public class DebugSettingsViewModel : INotifyPropertyChanged
    {
        public DebugSettings Settings { get; }
        public DebugSettingsViewModel(DebugSettings settings)
        {
            Settings = settings;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
