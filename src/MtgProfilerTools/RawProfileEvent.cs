using System.Text;

namespace MtgProfilerTools
{
    public enum ProfileEventType
    {
        StartMethod,
        EndMethod
    }

    public readonly struct RawProfileEvent
    {
        public RawProfileEvent(int threadId, long parentId, long activityId, long timestamp, ProfileEventType eventType, string name)
        {
            ThreadId = threadId;
            ParentId = parentId;
            ActivityId = activityId;
            Timestamp = timestamp;
            EventType = eventType;
            Name = name;
        }

        public int ThreadId { get; }

        public long ParentId { get; }

        public long ActivityId { get; }

        public long Timestamp { get; }

        public ProfileEventType EventType { get; }

        public string Name { get; }
    }
}
