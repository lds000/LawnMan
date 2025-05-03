using System.Collections.ObjectModel;

namespace BackyardBoss.Models
{
    /// <summary>
    /// Represents the entire sprinkler schedule configuration.
    /// Supports only a single program with global StartTimes and Sets.
    /// </summary>
    public class SprinklerSchedule
    {
        public ObservableCollection<string> StartTimes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<ProgramSet> Sets { get; set; } = new ObservableCollection<ProgramSet>();
    }
}
