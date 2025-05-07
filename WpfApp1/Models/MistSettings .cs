using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class MistSettings : INotifyPropertyChanged
{
    private int _durationMinutes = 2;
    private bool _time1030;
    private bool _time1330;
    private bool _time1600;
    private int? _pulseDurationMinutes;
    private int? _soakDurationMinutes;

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (_durationMinutes != value)
            {
                _durationMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("time_1030")]
    public bool Time1030
    {
        get => _time1030;
        set
        {
            if (_time1030 != value)
            {
                _time1030 = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("time_1330")]
    public bool Time1330
    {
        get => _time1330;
        set
        {
            if (_time1330 != value)
            {
                _time1330 = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("time_1600")]
    public bool Time1600
    {
        get => _time1600;
        set
        {
            if (_time1600 != value)
            {
                _time1600 = value;
                OnPropertyChanged();
            }
        }
    }

        [JsonPropertyName("pulse_duration_minutes")]
        public int? PulseDurationMinutes
        {
            get => _pulseDurationMinutes;
            set
            {
                if (_pulseDurationMinutes != value)
                {
                    _pulseDurationMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("soak_duration_minutes")]
        public int? SoakDurationMinutes
        {
            get => _soakDurationMinutes;
            set
            {
                if (_soakDurationMinutes != value)
                {
                    _soakDurationMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
