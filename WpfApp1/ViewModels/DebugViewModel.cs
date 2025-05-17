using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BackyardBoss.Models;
using BackyardBoss.Services;

namespace BackyardBoss.ViewModels
{
    public class DebugViewModel : INotifyPropertyChanged
    {
        public static DebugViewModel Current { get; private set; }
        public ObservableCollection<DebugInfo> DebugItems { get; } = new();
        public ObservableCollection<DebugInfo> FilteredDebugItems { get; } = new();

        public DebugViewModel()
        {
            Current = this;
            DebugLogger.Settings.PropertyChanged += (s, e) => ApplyFilter();
        }

        public void AddDebug(string message, string source = "", string details = "")
        {
            var info = new DebugInfo { Message = message, Source = source, Details = details };
            DebugItems.Add(info);
            ApplyFilter();
            OnPropertyChanged(nameof(DebugItems));
        }

        private void ApplyFilter()
        {
            FilteredDebugItems.Clear();
            foreach (var item in DebugItems.Reverse())
            {
                if (IsTypeEnabled(item.Source))
                    FilteredDebugItems.Add(item);
            }
            // Do NOT call OnPropertyChanged(nameof(FilteredDebugItems)) here
        }

        private bool IsTypeEnabled(string type)
        {
            var s = DebugLogger.Settings;
            return (type == "FileIO" && s.FileIO)
                || (type == "PropertyChange" && s.PropertyChanges)
                || (type == "VariableStatus" && s.VariableStatus)
                || (type == "Network" && s.Network)
                || (type == "AutoSave" && s.AutoSave)
                || (type == "PiCommunication" && s.PiCommunication)
                || (type == "Error" && s.Error)
                || (type == "CurrentIssue" && s.CurrentIssue);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
