using System;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class MoistureSensorReading
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("moisture")]
        public int Moisture { get; set; }

        [JsonPropertyName("wetness_percent")]
        public double WetnessPercent { get; set; }

        [JsonPropertyName("lux")]
        public double Lux { get; set; }

        [JsonPropertyName("sensor_id")]
        public string SensorId { get; set; }
    }
}
