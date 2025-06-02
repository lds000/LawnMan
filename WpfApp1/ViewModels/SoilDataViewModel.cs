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
                // Group by day/4-hour block, average wetness_percent, up to 10 days
                var grouped = readings
                    .Select(r => new
                    {
                        Date = DateTime.Parse(r.Timestamp),
                        Wetness = r.WetnessPercent
                    })
                    .GroupBy(x => new { x.Date.Date, Block = x.Date.Hour / 4 })
                    .OrderByDescending(g => g.Key.Date)
                    .ThenBy(g => g.Key.Block)
                    .Take(10 * 6) // up to 10 days, 6 blocks per day
                    .ToList();
                var bars = grouped
                    .GroupBy(g => g.Key.Date)
                    .Take(10)
                    .SelectMany(dayGroup =>
                        dayGroup.OrderBy(g => g.Key.Block).Select(g => new SoilBarData
                        {
                            Day = g.Key.Date,
                            Hour = g.Key.Block * 4,
                            Label = $"{g.Key.Date:ddd} {g.Key.Block * 4:00}:00",
                            MoisturePercent = (int)Math.Round(g.Average(x => x.Wetness) * 100)
                        })
                    ).ToList();
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
        public int MoisturePercent { get; set; } // 0-100
    }
}
