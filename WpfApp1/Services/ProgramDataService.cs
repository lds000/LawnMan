using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BackyardBoss.Models;

namespace BackyardBoss.Services
{
    public static class ProgramDataService
    {
        private static readonly string ScheduleFilePath = "sprinkler_schedule.json";

        public static async Task<SprinklerSchedule> LoadScheduleAsync()
        {
            if (!File.Exists(ScheduleFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"File not found: {ScheduleFilePath}");
                return new SprinklerSchedule(); // New empty if file missing
            }

            var json = await Task.Run(() => File.ReadAllText(ScheduleFilePath));
            var schedule = JsonConvert.DeserializeObject<SprinklerSchedule>(json);

            System.Diagnostics.Debug.WriteLine($"Programs loaded: {schedule?.Programs?.Count ?? 0}");

            return schedule ?? new SprinklerSchedule();
        }


        public static async Task SaveScheduleAsync(SprinklerSchedule schedule)
        {
            var json = JsonConvert.SerializeObject(schedule, Formatting.Indented);
            await Task.Run(() => File.ReadAllText(ScheduleFilePath));
        }
    }
}
