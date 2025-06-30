using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BackyardBoss.Models;

namespace BackyardBoss.Services
{
    /// <summary>
    /// Handles JSON-based load/save for a single sprinkler schedule.
    /// </summary>
    public static class ProgramDataService
    {
        private const string FilePath = "sprinkler_schedule.json";
        private const string PiIp = "100.116.147.6"; // Main Pi controller IP
        private const string PiScheduleUrl = $"http://{PiIp}:5000/schedule";

        public static async Task<SprinklerSchedule> LoadScheduleAsync()
        {
            // Try to load from Pi endpoint first
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(PiScheduleUrl);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var schedule = await JsonSerializer.DeserializeAsync<SprinklerSchedule>(stream);
                    if (schedule != null)
                        return schedule;
                }
            }
            catch
            {
                // Fallback to local file if network fails
            }

            // Fallback: load from local file
            if (!File.Exists(FilePath))
                return new SprinklerSchedule();

            using var fileStream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<SprinklerSchedule>(fileStream)
                   ?? new SprinklerSchedule();
        }

        public static async Task SaveScheduleAsync(SprinklerSchedule schedule)
        {
            // Ensure every mist.temperature_settings object has mist_enabled: true
            if (schedule?.Mist?.TemperatureSettings != null)
            {
                foreach (var setting in schedule.Mist.TemperatureSettings)
                {
                    setting.MistEnabled = true;
                }
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = null // Use property names as defined by JsonPropertyName
            };

            using var stream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(stream, schedule, options);
        }
    }
}
