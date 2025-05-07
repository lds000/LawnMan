// WeatherViewModel.cs
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace BackyardBoss.ViewModels
{
    public class WeatherViewModel : INotifyPropertyChanged
    {
        private string _temperature;
        private string _condition;
        private BitmapImage _weatherIcon;
        public WeatherViewModel()
        {
            _ = LoadWeatherAsync(); // fire-and-forget async load on startup
        }

        public string Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged(nameof(Temperature));
            }
        }

        public string Condition
        {
            get => _condition;
            set
            {
                _condition = value;
                OnPropertyChanged(nameof(Condition));
            }
        }

        public BitmapImage WeatherIcon
        {
            get => _weatherIcon;
            set
            {
                _weatherIcon = value;
                OnPropertyChanged(nameof(WeatherIcon));
            }
        }

        public async Task LoadWeatherAsync()
        {
            try
            {
                var apiKey = "cf5f2b7705dbc0348d0f8a773d5d2882";
                var zip = "83702"; // Boise ZIP
                var units = "imperial";
                var url = $"https://api.openweathermap.org/data/2.5/weather?zip={zip},us&units={units}&appid={apiKey}";

                using var client = new HttpClient();
                var response = await client.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<OpenWeatherResponse>(response);

                Temperature = $"{data.main.temp:F0} °F";
                Condition = data.weather[0].main;
                WeatherIcon = LoadIcon(data.weather[0].icon);
            }
            catch (Exception ex)
            {
                Temperature = "--";
                Condition = "Unavailable";
                WeatherIcon = null;
                Console.WriteLine("Weather error: " + ex.Message);
            }
        }

        private BitmapImage LoadIcon(string code)
        {
            try
            {
                string url = $"https://openweathermap.org/img/wn/{code}@2x.png";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private class OpenWeatherResponse
        {
            public MainInfo main
            {
                get; set;
            }
            public WeatherInfo[] weather
            {
                get; set;
            }
        }

        private class MainInfo
        {
            public double temp
            {
                get; set;
            }
        }

        private class WeatherInfo
        {
            public string main
            {
                get; set;
            }
            public string icon
            {
                get; set;
            }
        }
    }
}
