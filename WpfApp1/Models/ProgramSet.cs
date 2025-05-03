using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    public class ProgramSet
    {
        [JsonPropertyName("set_name")]
        public string SetName
        {
            get; set;
        } // e.g., "Set1", "Set2", "Set3"

        [JsonPropertyName("run_duration_minutes")]
        public int RunDurationMinutes
        {
            get; set;
        } // 0 means skip this set
    }
}
