using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace BackyardBoss.Models
{
    /// <summary>
    /// Represents the entire sprinkler schedule configuration.
    /// Supports only a single program with global StartTimes and Sets.
    /// </summary>
    public class SprinklerSchedule
    {
        [JsonPropertyName("start_times")]
        public ObservableCollection<string> StartTimes { get; set; } = new();

        [JsonPropertyName("sets")]
        public ObservableCollection<SprinklerSet> Sets { get; set; } = new();
    }
}

