using BackyardBoss.Data;
using System.Threading.Tasks;

namespace BackyardBoss
{
    public class MainWindowViewModel
    {
        // ...existing properties and methods...

        public async Task GenerateSampleSensorDatabaseAsync()
        {
            string dbPath = "sample_sensors.db";
            var repo = new SqliteSensorDataRepository(dbPath);
            await repo.InitializeAsync();
            await repo.InsertMonthSampleDataAsync();
        }
    }
}
