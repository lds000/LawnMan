using System.ComponentModel;

namespace BackyardBoss.Models
{
    public class DebugSettings : INotifyPropertyChanged
    {
        private bool _fileIO;
        private bool _propertyChanges;
        private bool _variableStatus;
        private bool _network;
        private bool _autoSave;
        private bool _piCommunication;
        private bool _error;
        private bool _currentIssue;

        public bool FileIO { get => _fileIO; set { _fileIO = value; OnPropertyChanged(nameof(FileIO)); } }
        public bool PropertyChanges { get => _propertyChanges; set { _propertyChanges = value; OnPropertyChanged(nameof(PropertyChanges)); } }
        public bool VariableStatus { get => _variableStatus; set { _variableStatus = value; OnPropertyChanged(nameof(VariableStatus)); } }
        public bool Network { get => _network; set { _network = value; OnPropertyChanged(nameof(Network)); } }
        public bool AutoSave { get => _autoSave; set { _autoSave = value; OnPropertyChanged(nameof(AutoSave)); } }
        public bool PiCommunication { get => _piCommunication; set { _piCommunication = value; OnPropertyChanged(nameof(PiCommunication)); } }
        public bool Error { get => _error; set { _error = value; OnPropertyChanged(nameof(Error)); } }
        public bool CurrentIssue { get => _currentIssue; set { _currentIssue = value; OnPropertyChanged(nameof(CurrentIssue)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
