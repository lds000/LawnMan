using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using BackyardBoss.ViewModels; // Required to call ProgramEditorViewModel.Current

public class StartTimeViewModel : INotifyPropertyChanged
{
    private string _time;
    private bool _isEnabled = true;

    public StartTimeViewModel()
    {
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsEnabled) || args.PropertyName == nameof(Time))
            {
                ProgramEditorViewModel.Current?.DebouncedSaveAndSendToPi();
            }
        };
    }

    [JsonPropertyName("time")]
    public string Time
    {
        get => _time;
        set
        {
            // Normalize to "HH:mm"
            if (TimeSpan.TryParse(value, out var ts))
                _time = ts.ToString(@"hh\:mm");
            else
                _time = value; // fallback to raw input if parsing fails

            OnPropertyChanged();
            OnPropertyChanged(nameof(ParsedTime));
        }
    }


    [JsonPropertyName("isEnabled")]
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    public TimeSpan ParsedTime
    {
        get
        {
            if (TimeSpan.TryParse(Time, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        set
        {
            Time = value.ToString(@"hh\:mm");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
