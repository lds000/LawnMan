using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackyardBoss.Models
{
    public class WateringLogEntry
    {
        public DateTime Date { get; set; }
        public string SetName { get; set; }
        public string Source { get; set; } // "SCHEDULED" or "MANUAL"
        public TimeSpan Duration { get; set; }
        public int DurationMinutes
        {
            get => (int)Duration.TotalMinutes;
            set => Duration = TimeSpan.FromMinutes(value);
        }
        public string Status { get; set; } // e.g. Completed, Error
        public string DateTimeDisplay => Date == DateTime.MinValue ? string.Empty : Date.ToString("MMM dd, yyyy h:mm tt");
    }
}
