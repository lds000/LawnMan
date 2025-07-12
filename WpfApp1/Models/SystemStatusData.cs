using System;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class SystemStatusData
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }
        [JsonPropertyName("ip")]
        public string Ip { get; set; }
        [JsonPropertyName("cpu_temp_c")]
        public double CpuTempC { get; set; }
        [JsonPropertyName("cpu_temp_warning")]
        public string CpuTempWarning { get; set; }
        [JsonPropertyName("mem")]
        public MemInfo Mem { get; set; }
        [JsonPropertyName("disk")]
        public DiskInfo Disk { get; set; }
        [JsonPropertyName("uptime_sec")]
        public double UptimeSec { get; set; }
        [JsonPropertyName("loadavg")]
        public LoadAvgInfo LoadAvg { get; set; }
        [JsonPropertyName("system_health")]
        public string SystemHealth { get; set; }
    }

    public class MemInfo
    {
        [JsonPropertyName("total_kb")]
        public int TotalKb { get; set; }
        [JsonPropertyName("free_kb")]
        public int FreeKb { get; set; }
    }

    public class DiskInfo
    {
        [JsonPropertyName("total_bytes")]
        public long TotalBytes { get; set; }
        [JsonPropertyName("free_bytes")]
        public long FreeBytes { get; set; }
    }

    public class LoadAvgInfo
    {
        [JsonPropertyName("1min")]
        public double OneMin { get; set; }
        [JsonPropertyName("5min")]
        public double FiveMin { get; set; }
        [JsonPropertyName("15min")]
        public double FifteenMin { get; set; }
    }
}
