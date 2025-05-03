using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

public class SprinklerSet : INotifyPropertyChanged
{
    private int _runDurationMinutes;

    [JsonPropertyName("set_name")]
    public string SetName
    {
        get; set;
    }

    [JsonPropertyName("run_duration_minutes")]
    public int RunDurationMinutes
    {
        get => _runDurationMinutes;
        set
        {
            if (_runDurationMinutes != value)
            {
                _runDurationMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
