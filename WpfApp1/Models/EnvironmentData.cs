using System;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class EnvironmentData
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("barometric_pressure")]
        public double BarometricPressure { get; set; }
    }

    public class PlantData
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("moisture")]
        public double Moisture { get; set; }

        [JsonPropertyName("lux")]
        public double Lux { get; set; }

        [JsonPropertyName("soil_temperature")]
        public double SoilTemperature { get; set; }
    }

    public class SetsData
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("set_name")]
        public string SetName { get; set; }

        [JsonPropertyName("pressure")]
        public double Pressure { get; set; }

        [JsonPropertyName("flow_litres")]
        public double FlowLitres { get; set; }

        [JsonPropertyName("flow_pulses")]
        public int FlowPulses { get; set; }

        [JsonPropertyName("pressure_kpa")]
        public double PressureKpa { get; set; }
    }
}