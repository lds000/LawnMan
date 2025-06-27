using System;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class MistStatus
    {
        [JsonPropertyName("current_temperature")]
        public double CurrentTemperature { get; set; }
        [JsonPropertyName("duration_minutes")]
        public double? DurationMinutes { get; set; }
        [JsonPropertyName("interval_minutes")]
        public int IntervalMinutes { get; set; }
        [JsonPropertyName("is_misting")]
        public bool IsMisting { get; set; }
        [JsonPropertyName("last_mist_event")]
        public DateTime? LastMistEvent { get; set; }
        [JsonPropertyName("next_mist_event")]
        public DateTime? NextMistEvent { get; set; }
    }
}
