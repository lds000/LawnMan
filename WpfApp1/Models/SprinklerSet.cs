using BackyardBoss.Commands;
using BackyardBoss.ViewModels;
using BackyardBoss.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    [JsonPropertyName("zone_id")]
    public int ZoneId { get; set; }

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
                ProgramEditorViewModel.Current?.DebouncedSaveAndSendToPi();
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
                ProgramEditorViewModel.Current?.DebouncedSaveAndSendToPi();
            }
        }
    }

    public ICommand OpenPulseKeypadCommand => new RelayCommand(_ =>
    {
        var keypad = new NumericKeypadWindow(PulseDurationMinutes.ToString());
        keypad.ValueSubmitted += result =>
        {
            if (int.TryParse(result, out int value))
                PulseDurationMinutes = value;
        };
        keypad.Cancelled += (s, e) =>
        {
            // Handle cancellation if needed (optional)
        };
        keypad.Show(); // non-modal
    });

    public ICommand OpenSoakKeypadCommand => new RelayCommand(_ =>
    {
        var keypad = new NumericKeypadWindow(SoakDurationMinutes.ToString());
        keypad.ValueSubmitted += result =>
        {
            if (int.TryParse(result, out int value))
                SoakDurationMinutes = value;
        };
        keypad.Show(); // non-modal
    });



    private bool _mode = true; // default to scheduled

    [JsonPropertyName("mode")]
    public bool Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
                ProgramEditorViewModel.Current?.DebouncedSaveAndSendToPi();
            }
        }
    }

    //create an image source from the Resources by title SetName.png (remove space in title)
    [JsonIgnore]
    public ImageSource SetIcon
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SetName))
                return null;

            // Remove spaces from SetName
            string iconName = SetName.Replace(" ", string.Empty);

            // Adjust the path as needed to match your project structure
            string uri = $"pack://application:,,,/Assets/SetIcons/{iconName}.png";

            try
            {
                return new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
            catch
            {
                return null; // Optionally, return a default/fallback image here
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
