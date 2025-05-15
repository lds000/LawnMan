using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

public class WeatherViewModel : INotifyPropertyChanged
{
    private string _temperature = "Loading...";
    private string _humidity = "";
    private string _windSpeed = "";
    private string _pressure = "";
    private string _condition = "";
    private string _weatherIcon = "Assets/Weather/default.png";

    public string TemperatureDisplay => $"Temp: {Temperature}";
    public string HumidityDisplay => $"Humidity: {Humidity}%";
    public string WindDisplay => $"Wind: {WindSpeed} mph";
    public string PressureDisplay => $"Pressure: {Pressure} hPa";

    public string Temperature
    {
        get => _temperature;
        set
        {
            if (_temperature != value)
            {
                _temperature = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemperatureDisplay));
            }
        }
    }

    public string Humidity
    {
        get => _humidity;
        set
        {
            if (_humidity != value)
            {
                _humidity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HumidityDisplay));
            }
        }
    }

    public string WindSpeed
    {
        get => _windSpeed;
        set
        {
            if (_windSpeed != value)
            {
                _windSpeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindDisplay));
            }
        }
    }

    public string Pressure
    {
        get => _pressure;
        set
        {
            if (_pressure != value)
            {
                _pressure = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PressureDisplay));
            }
        }
    }

    public string Condition
    {
        get => _condition;
        set
        {
            if (_condition != value)
            {
                _condition = value;
                OnPropertyChanged();
            }
        }
    }

    public string WeatherIcon
    {
        get => _weatherIcon;
        set
        {
            if (_weatherIcon != value)
            {
                _weatherIcon = value;
                OnPropertyChanged();
            }
        }
    }

    private Timer _refreshTimer;

    public WeatherViewModel()
    {
        Console.WriteLine("[DEBUG] WeatherViewModel constructor fired"); // Confirm this appears
        _refreshTimer = new Timer(600000); // Refresh every 10 minutes
        _refreshTimer.Elapsed += async (_, _) => await LoadWeatherAsync();
        _refreshTimer.Start();

        _ = LoadWeatherAsync(); // This must run here to load immediately
    }


    public async Task LoadWeatherAsync()
    {
        try
        {
            using var client = new HttpClient();
            string apiKey = "cf5f2b7705dbc0348d0f8a773d5d2882";
            string lat = "43.6150";
            string lon = "-116.2023";
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=imperial&appid={apiKey}";

            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            Temperature = $"{Math.Round(root.GetProperty("main").GetProperty("temp").GetDecimal())}°F";
            Humidity = root.GetProperty("main").GetProperty("humidity").GetRawText();
            WindSpeed = root.GetProperty("wind").GetProperty("speed").GetRawText();
            Pressure = root.GetProperty("main").GetProperty("pressure").GetRawText();

            var condition = root.GetProperty("weather")[0].GetProperty("description").GetString();
            var iconCode = root.GetProperty("weather")[0].GetProperty("icon").GetString();

            Condition = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(condition);
            WeatherIcon = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";

        }
        catch (Exception ex)
        {
            Temperature = "N/A";
            Humidity = WindSpeed = Pressure = "";
            Condition = "Unavailable";
            WeatherIcon = "Assets/Weather/default.png";
            Console.WriteLine($"Weather fetch failed: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
