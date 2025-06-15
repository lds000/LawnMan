using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

public class WeatherViewModel : INotifyPropertyChanged
{
    private string _temperature = "Loading...";
    private string _humidity = "";
    private string _windSpeed = "";
    private string _pressure = "";
    private string _condition = "";
    private string _weatherIcon = "pack://application:,,,/Assets/WeatherIcons/default.png";
    private string _feelsLike = "";
    private string _visibility = "";
    private string _sunrise = "";
    private string _sunset = "";

    public string TemperatureDisplay => $"Temp: {Temperature}";
    public string HumidityDisplay => $"Humidity: {Humidity}%";
    public string WindDisplay => $"Wind: {WindSpeed} mph";
    public string PressureDisplay => $"Pressure: {Pressure} hPa";
    public string FeelsLike => _feelsLike;
    public string Visibility => _visibility;
    public string Sunrise => _sunrise;
    public string Sunset => _sunset;

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

    private readonly DispatcherTimer _weatherTimer;

    public WeatherViewModel()
    {
        Console.WriteLine("[DEBUG] WeatherViewModel constructor fired"); // Confirm this appears

        // Set the interval (e.g., every 10 minutes)
        _weatherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(10)
        };
        _weatherTimer.Tick += async (s, e) => await LoadWeatherAsync();
        _weatherTimer.Start();

        // Initial load
        _ = LoadWeatherAsync();
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

            // Custom icon logic
            var tempF = root.GetProperty("main").GetProperty("temp").GetDecimal();
            if (tempF > 90)
            {
                WeatherIcon = "pack://application:,,,/Assets/WeatherIcons/hot.png";
            }
            else
            {
                WeatherIcon = $"pack://application:,,,/Assets/WeatherIcons/{iconCode}.png";
            }

            // New: Feels Like
            if (root.GetProperty("main").TryGetProperty("feels_like", out var feelsLikeProp))
                _feelsLike = $"{Math.Round(feelsLikeProp.GetDecimal())}°F";
            else
                _feelsLike = "-";
            OnPropertyChanged(nameof(FeelsLike));

            // New: Visibility (convert meters to miles)
            if (root.TryGetProperty("visibility", out var visProp))
            {
                double meters = visProp.GetDouble();
                double miles = meters / 1609.34;
                _visibility = $"{miles:F1} mi";
            }
            else
                _visibility = "-";
            OnPropertyChanged(nameof(Visibility));

            // New: Sunrise/Sunset
            if (root.GetProperty("sys").TryGetProperty("sunrise", out var sunriseProp))
            {
                var sunrise = DateTimeOffset.FromUnixTimeSeconds(sunriseProp.GetInt64()).ToLocalTime();
                _sunrise = sunrise.ToString("h:mm tt");
            }
            else
                _sunrise = "-";
            OnPropertyChanged(nameof(Sunrise));

            if (root.GetProperty("sys").TryGetProperty("sunset", out var sunsetProp))
            {
                var sunset = DateTimeOffset.FromUnixTimeSeconds(sunsetProp.GetInt64()).ToLocalTime();
                _sunset = sunset.ToString("h:mm tt");
            }
            else
                _sunset = "-";
            OnPropertyChanged(nameof(Sunset));
        }
        catch (Exception ex)
        {
            Temperature = "N/A";
            Humidity = WindSpeed = Pressure = "";
            Condition = "Unavailable";
            // Use a pack URI for the fallback icon so WPF can resolve it as a resource
            WeatherIcon = "pack://application:,,,/Assets/WeatherIcons/default.png";
            _feelsLike = _visibility = _sunrise = _sunset = "-";
            OnPropertyChanged(nameof(FeelsLike));
            OnPropertyChanged(nameof(Visibility));
            OnPropertyChanged(nameof(Sunrise));
            OnPropertyChanged(nameof(Sunset));
            Console.WriteLine($"Weather fetch failed: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
