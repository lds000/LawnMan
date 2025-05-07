using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using WpfApp1;

namespace SprinklerStatusViewer.ViewModels
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _logLines;
        public ObservableCollection<string> LogLines
        {
            get => _logLines;
            set
            {
                _logLines = value;
                OnPropertyChanged();
            }
        }

        private readonly Timer _refreshTimer;
        private readonly HttpClient _httpClient = new();

        public StatusViewModel()
        {
            LogLines = new ObservableCollection<string>();
            _refreshTimer = new Timer(5000); // refresh every 5 seconds
            _refreshTimer.Elapsed += async (s, e) => await RefreshLogAsync();
            _refreshTimer.Start();
        }

        public async Task RefreshLogAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://192.168.68.103:5000/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("log", out var logArray))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            LogLines.Clear();
                            foreach (var line in logArray.EnumerateArray())
                                LogLines.Add(line.GetString());
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LogLines.Clear();
                    LogLines.Add("[ERROR] " + ex.Message);
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
