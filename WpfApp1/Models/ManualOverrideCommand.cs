using System.Collections.Generic;

namespace BackyardBoss.Models
{
    public class ManualOverrideCommand
    {
        public ManualRun ManualRun
        {
            get; set;
        }
    }

    public class ManualRun
    {
        public List<string> Sets { get; set; } = new List<string>();
        public int DurationMinutes
        {
            get; set;
        }
    }
}
