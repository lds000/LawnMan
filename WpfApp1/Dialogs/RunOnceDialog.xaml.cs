using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using BackyardBoss.Models;

namespace BackyardBoss.Dialogs
{
    public partial class RunOnceDialog : Window
    {
        public ObservableCollection<RunOnceSetViewModel> SetOverrides { get; set; } = new();

        public RunOnceDialog(ObservableCollection<SprinklerSet> sets)
        {
            InitializeComponent();
            foreach (var set in sets)
            {
                SetOverrides.Add(new RunOnceSetViewModel
                {
                    SetName = set.SetName,
                    Duration = 0
                });
            }
            DataContext = this;
        }

        public ObservableCollection<RunOnceSetViewModel> GetOverrides() => SetOverrides;

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    public class RunOnceSetViewModel : INotifyPropertyChanged
    {
        private int _duration;

        public string SetName
        {
            get; set;
        }

        public int Duration
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    OnPropertyChanged(nameof(Duration));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
