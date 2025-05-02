using System.Threading.Tasks;
using BackyardBoss.Models;

namespace BackyardBoss.Services
{
    public interface ISprinklerDataService
    {
        Task<SprinklerSchedule> LoadScheduleAsync();
        Task SaveScheduleAsync(SprinklerSchedule schedule);
        Task<ManualOverrideCommand> LoadManualCommandAsync();
        Task SaveManualCommandAsync(ManualOverrideCommand command);
    }
}
