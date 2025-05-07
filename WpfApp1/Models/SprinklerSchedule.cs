using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

public class SprinklerSchedule : INotifyPropertyChanged
{
    private double _seasonalAdjustment = 1.0;

    [JsonPropertyName("start_times")]
    public ObservableCollection<StartTimeViewModel> StartTimes { get; set; } = new();


    [JsonPropertyName("sets")]
    public ObservableCollection<SprinklerSet> Sets { get; set; } = new();

    [JsonPropertyName("seasonal_adjustment")]
    public double SeasonalAdjustment
    {
        get => _seasonalAdjustment;
        set
        {
            if (_seasonalAdjustment != value)
            {
                _seasonalAdjustment = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("schedule_days")]
    public ObservableCollection<bool> ScheduleDays { get; set; } = new(Enumerable.Repeat(false, 14));

    [JsonPropertyName("mist")]
    public MistSettings Mist { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}
