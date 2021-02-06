using System;

namespace MtgProfileAnalyzer
{
    internal class SessionData
    {
        public string File { get; set; }

        public DateTime Timestamp { get; set; }

        public long StopwatchFrequency { get; set; }

        public long InitialTimestamp { get; set; }
    }
}
