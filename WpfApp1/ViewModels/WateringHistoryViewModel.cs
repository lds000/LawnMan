using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackyardBoss.ViewModels
{
    public class WateringLogEntry
    {
        public DateTime Date
        {
            get; set;
        }
        public string SetName
        {
            get; set;
        }
        public string Source
        {
            get; set;
        } // "SCHEDULED" or "MANUAL"
        public TimeSpan Duration
        {
            get; set;
        }
    }

    public class SetLog
    {
        public string SetName
        {
            get; set;
        }
        public double ScheduledMinutes
        {
            get; set;
        }
        public double ManualMinutes
        {
            get; set;
        }
    }

    public class DayLog
    {
        public DateTime Date
        {
            get; set;
        }
        public double ScheduledMinutes
        {
            get; set;
        }
        public double ManualMinutes
        {
            get; set;
        }
    }


    public class WateringHistoryViewModel : INotifyPropertyChanged
    {
        public Dictionary<string, List<BackyardBoss.ViewModels.DayLog>> SetGraphs { get; set; } = new();

        public WateringHistoryViewModel()
        {
            LoadAsync();
        }

        private async void LoadAsync()
        {
            var logEntries = await LoadFromRemoteLogAsync();

            var grouped = logEntries
                .GroupBy(e => e.SetName)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {   
                        var days = Enumerable.Range(0, 7)
                            .Select(offset => DateTime.Today.AddDays(-6 + offset))
                            .ToList();

                        return days.Select(day => {
                            var entries = g.Where(e => e.Date == day).ToList();
                            return new BackyardBoss.ViewModels.DayLog
                            {
                                Date = day,
                                ScheduledMinutes = entries.Where(e => e.Source == "SCHEDULED").Sum(e => e.Duration.TotalMinutes),
                                ManualMinutes = entries.Where(e => e.Source == "MANUAL").Sum(e => e.Duration.TotalMinutes)
                            };
                        }).ToList();

                    });

            SetGraphs = grouped;
            OnPropertyChanged(nameof(SetGraphs));
        }


        private async Task<List<WateringLogEntry>> LoadFromRemoteLogAsync()
        {
            var list = new List<WateringLogEntry>();
            try
            {
                using var client = new HttpClient();
                var logText = await client.GetStringAsync("http://100.116.147.6:5000/history-log");

                foreach (var line in logText.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var parts = line.Split(' ');
                        var date = DateTime.Parse(parts[0]);

                        var setName = string.Join(" ", parts.Skip(1).Take(parts.Length - 6)); // Fix is here
                        var source = parts[^5]; // "SCHEDULED" or "MANUAL"
                        var start = TimeSpan.Parse(parts[^3]);
                        var stop = TimeSpan.Parse(parts[^1]);
                        var duration = stop - start;

                        list.Add(new WateringLogEntry
                        {
                            Date = date,
                            SetName = setName,
                            Source = source,
                            Duration = duration
                        });

                    }

                    catch { }
                }
            }
            catch { }
            return list;
        }

        private async Task<List<string>> LoadSetNamesFromJsonAsync(string localPath)
        {
            try
            {
                if (!File.Exists(localPath))
                    return new List<string>();

                using var stream = File.OpenRead(localPath);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (!jsonDoc.RootElement.TryGetProperty("sets", out var setsElement))
                    return new List<string>();

                return setsElement
                    .EnumerateArray()
                    .Select(s => s.TryGetProperty("set_name", out var nameProp) ? nameProp.GetString() : null)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
