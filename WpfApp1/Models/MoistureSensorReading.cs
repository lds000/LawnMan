using System;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class MoistureSensorReading
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("moisture")]
        public double Moisture { get; set; }
    }
}
