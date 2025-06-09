using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using BackyardBoss.Models;

namespace BackyardBoss.ViewModels
{
    public class SoilDataViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SoilBarData> Bars { get; set; } = new();
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsLoading { get; set; }
        public string Error { get; set; }

        public async Task LoadSoilDataAsync()
        {
            try
            {
                IsLoading = true;
                Error = null;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                using var client = new HttpClient();
                var json = await client.GetStringAsync("http://100.116.147.6:5000/soil-history");
                var readings = JsonSerializer.Deserialize<MoistureSensorReading[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (readings == null) return;

                var bars = readings
                    .Select(r =>
                    {
                        var dt = DateTime.Parse(r.Timestamp);
                        return new SoilBarData
                        {
                            Day = dt.Date,
                            Hour = dt.Hour,
                            Label = $"{dt:ddd} {dt:HH}:{dt:mm} {dt:ss}" ,
                            MoisturePercent = r.Moisture
                        };
                    })
                    .ToList();

                Bars = new ObservableCollection<SoilBarData>(bars);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bars)));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
            }
            finally
            {
                IsLoading = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
            }
        }
    }

    public class SoilBarData
    {
        public DateTime Day { get; set; }
        public int Hour { get; set; }
        public string Label { get; set; } // e.g. "Mon"
        public double MoisturePercent { get; set; } // 0-100, now double for precision
    }
}
