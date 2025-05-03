using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

public class TimePickerViewModel : INotifyPropertyChanged
{
    public ObservableCollection<int> Hours { get; } = new ObservableCollection<int>(Enumerable.Range(0, 24));
    public ObservableCollection<int> Minutes { get; } = new ObservableCollection<int>(Enumerable.Range(0, 60));

    private int _selectedHour;
    public int SelectedHour
    {
        get => _selectedHour;
        set
        {
            if (_selectedHour != value)
            {
                _selectedHour = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTime));
            }
        }
    }

    private int _selectedMinute;
    public int SelectedMinute
    {
        get => _selectedMinute;
        set
        {
            if (_selectedMinute != value)
            {
                _selectedMinute = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTime));
            }
        }
    }

    public TimeSpan SelectedTime => new TimeSpan(SelectedHour, SelectedMinute, 0);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
