using System;
using System.Text.Json.Serialization;

public class PressureAvgData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonPropertyName("avg_psi")]
    public double AvgPressurePsi { get; set; } // Map to pressure_psi column
    [JsonPropertyName("avg_flow_lpm")]
    public double AvgFlowLpm { get; set; } // Map to flow_litres_per_minute column
    public int ZoneId { get; set; }
    [JsonPropertyName("samples")]
    public int NumSamples { get; set; }
}
