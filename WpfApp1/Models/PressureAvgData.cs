using System;
using System.Text.Json.Serialization;

public class PressureAvgData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonPropertyName("pressure_psi")]
    public double AvgPressurePsi { get; set; } // Map to pressure_psi column
    [JsonPropertyName("flow_lpm")]
    public double AvgFlowLpm { get; set; } // Map to flow_lpm column
    [JsonPropertyName("zone_id")]
    public int ZoneId { get; set; }
    [JsonPropertyName("num_samples")]
    public int NumSamples { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
}
