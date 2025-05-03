using System.IO;
using System.Text.Json;
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

        public static async Task<SprinklerSchedule> LoadScheduleAsync()
        {
            if (!File.Exists(FilePath))
                return new SprinklerSchedule();

            using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<SprinklerSchedule>(stream)
                   ?? new SprinklerSchedule();
        }

        public static async Task SaveScheduleAsync(SprinklerSchedule schedule)
        {
            using var stream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(stream, schedule, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
