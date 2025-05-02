using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace BackyardBoss.Models
{
    public class SprinklerSchedule
    {
        [JsonProperty("seasonal_adjustment")]
        public double SeasonalAdjustment { get; set; } = 1.0;

        [JsonProperty("programs")]
        public ObservableCollection<WateringProgram> Programs { get; set; } = new ObservableCollection<WateringProgram>();
    }
}
