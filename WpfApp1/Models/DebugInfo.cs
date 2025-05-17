using System;

namespace BackyardBoss.Models
{
    public class DebugInfo
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; }
        public string Source { get; set; }
        public string Details { get; set; }
    }
}
