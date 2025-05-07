using System.ComponentModel;
using System.Runtime.CompilerServices;

public class RunOnceZoneViewModel : INotifyPropertyChanged
{
    public string SetName
    {
        get; set;
    }

    private int _duration;
    public int Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
