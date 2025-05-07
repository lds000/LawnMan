using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

public class WeatherViewModel : INotifyPropertyChanged
{
    private string _temperature = "Loading...";
    private string _condition = "";
    private string _weatherIcon = "Assets/Weather/default.png";

    public string Temperature
    {
        get => _temperature; set
        {
            _temperature = value;
            OnPropertyChanged();
        }
    }
    public string Condition
    {
        get => _condition; set
        {
            _condition = value;
            OnPropertyChanged();
        }
    }
    public string WeatherIcon
    {
        get => _weatherIcon; set
        {
            _weatherIcon = value;
            OnPropertyChanged();
        }
    }

    private Timer _refreshTimer;

    public WeatherViewModel()
    {
        _refreshTimer = new Timer(600000); // Refresh every 10 minutes
        _refreshTimer.Elapsed += async (_, _) => await LoadWeatherAsync();
        _refreshTimer.Start();

        _ = LoadWeatherAsync(); // Initial load
    }

    private async Task LoadWeatherAsync()
    {
        try
        {
            using var client = new HttpClient();
            // Example API (OpenWeatherMap One Call 3.0 with key)
            string apiKey = "cf5f2b7705dbc0348d0f8a773d5d2882";
            string lat = "43.6150";
            string lon = "-116.2023";
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=imperial&appid={apiKey}";

            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var temp = root.GetProperty("main").GetProperty("temp").GetDecimal();
            var condition = root.GetProperty("weather")[0].GetProperty("description").GetString();
            var iconCode = root.GetProperty("weather")[0].GetProperty("icon").GetString();

            Temperature = $"{Math.Round(temp)}°F";
            Condition = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(condition);
            WeatherIcon = $"Assets/Weather/{iconCode}.png";
        }
        catch (Exception ex)
        {
            Temperature = "N/A";
            Condition = "Unavailable";
            WeatherIcon = "Assets/Weather/default.png";
            Console.WriteLine($"Weather fetch failed: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
