using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace BackyardBoss.Data
{
    public class SqliteSensorDataRepository
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public SqliteSensorDataRepository(string dbPath)
        {
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath}";
        }

        public async Task InitializeAsync()
        {
            if (!File.Exists(_dbPath))
            {
                await using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE sensor_readings (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp DATETIME NOT NULL,
                    zone_id INTEGER NOT NULL,
                    pressure_psi REAL,
                    flow_lpm REAL,
                    flow_total_liters REAL
                );
                CREATE TABLE IF NOT EXISTS pressure_avg (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp DATETIME NOT NULL,
                    avg_pressure_psi REAL NOT NULL,
                    num_samples INTEGER NOT NULL,
                    version TEXT
                );";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertSampleDataAsync()
        {
            var zoneIds = new[] { 1, 2, 3 };
            var rand = new Random();
            var now = DateTime.Now;
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            for (int i = 0; i < 30; i++)
            {
                var zoneId = zoneIds[rand.Next(zoneIds.Length)];
                var timestamp = now.AddMinutes(-i * 60);
                var pressure = 35 + rand.NextDouble() * 10;
                var flow = 2 + rand.NextDouble();
                var total = flow * rand.Next(5, 15);
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO sensor_readings (timestamp, zone_id, pressure_psi, flow_lpm, flow_total_liters) VALUES (@timestamp, @zone_id, @pressure, @flow, @totalLiters)";
                cmd.Parameters.AddWithValue("@timestamp", timestamp);
                cmd.Parameters.AddWithValue("@zone_id", zoneId);
                cmd.Parameters.AddWithValue("@pressure", pressure);
                cmd.Parameters.AddWithValue("@flow", flow);
                cmd.Parameters.AddWithValue("@totalLiters", total);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertMonthSampleDataAsync()
        {
            // Use integer-based zone IDs only
            var zoneMap = new[]
            {
                1, // Front Lawn
                2, // Backyard
                3  // Garden
            };
            var rand = new Random();
            var now = DateTime.Now;
            var start = now.AddDays(-30);
            var readingsPerDay = 6; // 2 per set per day
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            for (int day = 0; day < 30; day++)
            {
                var date = start.AddDays(day);
                foreach (var zoneId in zoneMap)
                {
                    for (int r = 0; r < 2; r++) // 2 readings per set per day
                    {
                        var timestamp = date.AddHours(6 + r * 12); // e.g. 6am, 6pm
                        var pressure = 35 + rand.NextDouble() * 10;
                        var flow = 2 + rand.NextDouble();
                        var totalLiters = flow * rand.Next(5, 15);
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"INSERT INTO sensor_readings (timestamp, zone_id, pressure_psi, flow_lpm, flow_total_liters) VALUES (@timestamp, @zone_id, @pressure, @flow, @totalLiters)";
                        cmd.Parameters.AddWithValue("@timestamp", timestamp);
                        cmd.Parameters.AddWithValue("@zone_id", zoneId);
                        cmd.Parameters.AddWithValue("@pressure", pressure);
                        cmd.Parameters.AddWithValue("@flow", flow);
                        cmd.Parameters.AddWithValue("@totalLiters", totalLiters);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<List<SensorReading>> GetAllReadingsAsync()
        {
            var result = new List<SensorReading>();
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT timestamp, zone_id, pressure_psi, flow_lpm, flow_total_liters FROM sensor_readings ORDER BY timestamp DESC";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new SensorReading
                {
                    Timestamp = reader.GetDateTime(0),
                    ZoneId = reader.GetInt32(1),
                    PressurePsi = reader.GetDouble(2),
                    FlowLpm = reader.GetDouble(3),
                    FlowTotalLiters = reader.GetDouble(4)
                });
            }
            return result;
        }

        public async Task InsertPressureAvgAsync(DateTime timestamp, double avgPressurePsi, int numSamples, string version)
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO pressure_avg (timestamp, avg_pressure_psi, num_samples, version) VALUES (@timestamp, @avg, @samples, @version)";
            cmd.Parameters.AddWithValue("@timestamp", timestamp);
            cmd.Parameters.AddWithValue("@avg", avgPressurePsi);
            cmd.Parameters.AddWithValue("@samples", numSamples);
            cmd.Parameters.AddWithValue("@version", version ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<PressureAvgData>> GetPressureAvgLast7DaysAsync()
        {
            var result = new List<PressureAvgData>();
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT timestamp, avg_pressure_psi, num_samples, version FROM pressure_avg WHERE timestamp >= @cutoff ORDER BY timestamp ASC";
            cmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-7));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new PressureAvgData
                {
                    Timestamp = reader.GetDateTime(0),
                    AvgPressurePsi = reader.GetDouble(1),
                    NumSamples = reader.GetInt32(2),
                    Version = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return result;
        }

        /// <summary>
        /// Returns all rows from the sensor_readings table without grouping by 5-minute intervals.
        /// </summary>
        public async Task<List<PressureAvgData>> GetPressureAvg5MinAggregatesAsync()
        {
            var result = new List<PressureAvgData>();
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();

            // Retrieve all rows without grouping
            cmd.CommandText = @"
                SELECT 
                    timestamp,
                    pressure_psi AS avg_pressure_psi,
                    1 AS num_samples
                FROM sensor_readings
                WHERE timestamp >= @cutoff
                ORDER BY timestamp ASC;";

            cmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-7));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new PressureAvgData
                {
                    Timestamp = reader.GetDateTime(0),
                    AvgPressurePsi = reader.GetDouble(1),
                    NumSamples = reader.GetInt32(2),
                    Version = null // Not used for individual rows
                });
            }

            Debug.WriteLine($"GetPressureAvg5MinAggregatesAsync retrieved {result.Count} rows.");
            return result;
        }

        /// <summary>
        /// Inserts a single sensor reading into the sensor_readings table.
        /// </summary>
        public async Task InsertSensorReadingAsync(DateTime timestamp, int zoneId, double pressurePsi, double flowLpm, double flowTotalLiters)
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO sensor_readings (timestamp, zone_id, pressure_psi, flow_lpm, flow_total_liters) 
                                VALUES (@timestamp, @zone_id, @pressure, @flow, @totalLiters)";
            cmd.Parameters.AddWithValue("@timestamp", timestamp);
            cmd.Parameters.AddWithValue("@zone_id", zoneId);
            cmd.Parameters.AddWithValue("@pressure", pressurePsi);
            cmd.Parameters.AddWithValue("@flow", flowLpm);
            cmd.Parameters.AddWithValue("@totalLiters", flowTotalLiters);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public int ZoneId { get; set; }
        public double PressurePsi { get; set; }
        public double FlowLpm { get; set; }
        public double FlowTotalLiters { get; set; }
    }

    public class PressureAvgData
    {
        public DateTime Timestamp { get; set; }
        public double AvgPressurePsi { get; set; }
        public int NumSamples { get; set; }
        public string Version { get; set; }
    }
}
