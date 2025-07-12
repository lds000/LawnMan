namespace BackyardBoss.Models
{
    public class ZoneStatus
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } // e.g. Idle, Soak, Running
    }
}
