using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace BackyardBoss.Data
{
    public class SensorDataRepository
    {
        private readonly string _connectionString;

        public SensorDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertSensorReadingAsync(DateTime timestamp, string zone, double pressurePsi, double flowLpm, double flowTotalLiters)
        {
            const string query = @"INSERT INTO sensor_readings (timestamp, zone, pressure_psi, flow_lpm, flow_total_liters) VALUES (@timestamp, @zone, @pressure, @flow, @totalLiters)";
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@timestamp", timestamp);
            cmd.Parameters.AddWithValue("@zone", zone);
            cmd.Parameters.AddWithValue("@pressure", pressurePsi);
            cmd.Parameters.AddWithValue("@flow", flowLpm);
            cmd.Parameters.AddWithValue("@totalLiters", flowTotalLiters);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
