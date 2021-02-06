using System;

namespace MtgProfileAnalyzer
{
    internal class ProfileEvent
    {
        public ProfileEvent()
        {
        }

        public int ThreadId { get; set; }

        public long Id { get; set; }

        public long ParentId { get; set; }

        public string Name { get; set; }

        public long Start { get; set; }

        public long End { get; set; }

        public TimeSpan Duration { get; set; }

        public ProfileEvent Parent { get; set; }
    }
}
