using BackyardBoss.ViewModels;
using System;
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
                OnPropertyChanged(nameof(SeasonallyAdjustedMinutes)); // <- This is the fix
            }
        }
    }

    private int? _pulseDurationMinutes;
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

    private int? _soakDurationMinutes;
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


    private string _mode = "scheduled"; // default to scheduled

    [JsonPropertyName("mode")]
    public string Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
            }
        }
    }


    public int SeasonallyAdjustedMinutes
    {
        get => (int)Math.Round(RunDurationMinutes * ProgramEditorViewModel.Current?.SeasonalAdjustment ?? 1.0);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
