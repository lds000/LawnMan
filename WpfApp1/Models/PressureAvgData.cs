using System;
using System.Text.Json.Serialization;

public class PressureAvgData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonPropertyName("avg_pressure_psi")]
    public double AvgPressurePsi { get; set; }
    [JsonPropertyName("num_samples")]
    public int NumSamples { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
}
