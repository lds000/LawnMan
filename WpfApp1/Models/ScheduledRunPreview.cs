using System;

namespace BackyardBoss.Models
{
    public class ScheduledRunPreview
    {
        public string SetName
        {
            get; set;
        }
        public string StartTime
        {
            get; set;
        } // "HH:mm" string
        public int RunDurationMinutes
        {
            get; set;
        }
    }
}
