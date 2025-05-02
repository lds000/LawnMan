using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class StartTimeViewModel : INotifyPropertyChanged
{
    private string _time = "06:00";
    public string Time
    {
        get => _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ParsedTime));
            }
        }
    }

    public TimeSpan ParsedTime
    {
        get => TimeSpan.TryParse(Time, out var ts) ? ts : TimeSpan.Zero;
        set
        {
            Time = value.ToString(@"hh\:mm");
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(ParsedTime));
        }
    }

    private bool _isPickerVisible;
    public bool IsPickerVisible
    {
        get => _isPickerVisible;
        set
        {
            if (_isPickerVisible != value)
            {
                _isPickerVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string property = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
