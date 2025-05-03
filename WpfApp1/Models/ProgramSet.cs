using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class ProgramSet
    {
        [JsonPropertyName("set_name")]
        public string SetName
        {
            get; set;
        }

        [JsonPropertyName("run_duration_minutes")]
        public int RunDurationMinutes
        {
            get; set;
        }

        [JsonPropertyName("start_times")]
        public List<string> StartTimes { get; set; } = new();  // Add this line
    }
}
