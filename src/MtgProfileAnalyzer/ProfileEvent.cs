using System;

namespace MtgProfileAnalyzer
{
    internal class ProfileEvent
    {
        public ProfileEvent()
        {
        }

        public ProfileEventType EventType { get; set; }

        public int ThreadId { get; set; }

        public long Id { get; set; }

        public long ParentId { get; set; }

        public string Name { get; set; }

        public long RawTimestamp { get; set; }

        public TimeSpan Offset { get; set; }
    }
}
