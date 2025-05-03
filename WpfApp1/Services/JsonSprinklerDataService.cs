using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Shapes;
using BackyardBoss.Models;

namespace BackyardBoss.Services
{
    public class JsonSprinklerDataService : ISprinklerDataService
    {
        private readonly string _scheduleFilePath;
        private readonly string _manualCommandFilePath;

        public JsonSprinklerDataService(string scheduleFilePath, string manualCommandFilePath)
        {
            _scheduleFilePath = scheduleFilePath;
            _manualCommandFilePath = manualCommandFilePath;
        }

        public async Task<SprinklerSchedule> LoadScheduleAsync()
        {
            if (!File.Exists(_scheduleFilePath))
                return new SprinklerSchedule();  // Return empty if missing

            var json = await Task.Run(() => File.ReadAllText(_scheduleFilePath));
            return JsonSerializer.Deserialize<SprinklerSchedule>(json) ?? new SprinklerSchedule();
        }

        public async Task SaveScheduleAsync(SprinklerSchedule schedule)
        {
            var json = JsonSerializer.Serialize(schedule, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await Task.Run(() => File.ReadAllText(_scheduleFilePath));
        }

        public async Task<ManualOverrideCommand> LoadManualCommandAsync()
        {
            if (!File.Exists(_manualCommandFilePath))
                return new ManualOverrideCommand();  // Return empty if missing

            var json = await Task.Run(() => File.ReadAllText(_manualCommandFilePath));
            return JsonSerializer.Deserialize<ManualOverrideCommand>(json)
                   ?? new ManualOverrideCommand();
        }

        public async Task SaveManualCommandAsync(ManualOverrideCommand command)
        {

            var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await Task.Run(() => File.ReadAllText(_manualCommandFilePath));
        }
    }
}
