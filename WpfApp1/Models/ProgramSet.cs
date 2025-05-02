using Newtonsoft.Json;

namespace BackyardBoss.Models
{
    public class ProgramSet
    {
        [JsonProperty("set_name")]
        public string SetName
        {
            get; set;
        } // e.g., "Set1", "Set2", "Set3"

        [JsonProperty("run_duration_minutes")]
        public int RunDurationMinutes
        {
            get; set;
        } // 0 means skip this set
    }
}
