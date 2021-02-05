using System.Text;

namespace MtgProfilerTools
{
    internal enum ProfileEventType
    {
        StartMethod,
        EndMethod
    }

    internal struct ProfileEvent
    {
        public ProfileEvent(int threadId, long parentId, long activityId, long timestamp, ProfileEventType eventType, string name)
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

        public int ToBytes(byte[] buffer, int offset)
        {
            int currentOffset = offset;
            currentOffset = BitOperations.ToBytes(ThreadId, buffer, currentOffset);
            currentOffset = BitOperations.ToBytes(ParentId, buffer, currentOffset);
            currentOffset = BitOperations.ToBytes(ParentId, buffer, currentOffset);
            currentOffset = BitOperations.ToBytes(Timestamp, buffer, currentOffset);
            currentOffset = BitOperations.ToBytes((int)EventType, buffer, currentOffset);
            currentOffset = BitOperations.ToBytes(Name.Length, buffer, currentOffset);
            int stringBytes = Encoding.UTF8.GetBytes(Name, 0, Name.Length, buffer, currentOffset);
            int dataCount = (currentOffset - offset) + stringBytes;
            return dataCount;
        }
    }
}
