using System;
using System.Collections.Generic;
using System.IO;

namespace MtgProfileAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Specify an input file");
                return;
            }

            string path = args[0];
            if (!File.Exists(path))
            {
                Console.WriteLine("File does not exist");
                return;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            var header = reader.ReadBytes(64);
            var mtg = header[0..4];
            var version = header[4..8];
            var startDateTimeTicks = BitConverter.ToInt64(header[8..16]);
            var frequency = BitConverter.ToInt64(header[16..24]);
            var startTs = BitConverter.ToInt64(header[24..32]);

            var builder = new ProfileEventCollectionBuilder();
            builder.StopwatchFrequency = frequency;
            int records = 0;
            while (true)
            {
                try
                {
                    int threadId = reader.ReadInt32();
                    long parentId = reader.ReadInt64();
                    long id = reader.ReadInt64();
                    long ts = reader.ReadInt64();
                    var eventType = (ProfileEventType)reader.ReadInt32();
                    string name = reader.ReadString();

                    if (eventType == ProfileEventType.StartMethod)
                    {
                        builder.AddStart(ts, id, threadId, parentId, name);
                    }
                    else if (eventType == ProfileEventType.EndMethod)
                    {
                        builder.SetEnd(id, ts);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown event type {eventType} at {ts}");
                    }

                    records++;
                }
                catch (Exception)
                {
                    break;
                }
            }

            Console.WriteLine($"Read {records} records");
            var collection = builder.Build();
            var threadData = collection.GetThreadData();
            foreach (var thread in threadData)
            {
                Console.WriteLine($"ThreadId: {thread.Id}, Events: {thread.EventCount}");
            }
        }
    }

    class ProfileEventCollection
    {
        private Dictionary<int, List<ProfileEvent>> _byThread;
        private List<ThreadSummaryData> _threadSummaries;

        public ProfileEventCollection(IEnumerable<ProfileEvent> events)
        {
            _byThread = new Dictionary<int, List<ProfileEvent>>();

            foreach (var @event in events)
            {
                if (!_byThread.TryGetValue(@event.ThreadId, out var list))
                {
                    list = new List<ProfileEvent>();
                    _byThread.Add(@event.ThreadId, list);
                }

                list.Add(@event);
            }

            _threadSummaries = new List<ThreadSummaryData>();
            foreach (var (tid, list) in _byThread)
            {
                var summary = new ThreadSummaryData()
                {
                    Id = tid,
                    EventCount = list.Count
                };

                _threadSummaries.Add(summary);
            }
        }

        public IReadOnlyList<ThreadSummaryData> GetThreadData()
        {
            return _threadSummaries;
        }
    }

    class ProfileEventCollectionBuilder
    {
        private readonly Dictionary<long, ProfileEvent> _profileEvents;

        public ProfileEventCollectionBuilder()
        {
            _profileEvents = new Dictionary<long, ProfileEvent>();
        }

        public long StopwatchFrequency { get; set; }

        public void Add(ProfileEvent @event)
        {
            _profileEvents.Add(@event.Id, @event);
        }

        public void AddStart(long timestamp, long id, int threadId, long parentId, string name)
        {
            var profEvent = new ProfileEvent()
            {
                ThreadId = threadId,
                ParentId = parentId,
                Id = id,
                Name = name,
            };

            _profileEvents.Add(id, profEvent);
        }

        public bool SetEnd(long timestamp, long id)
        {
            if (_profileEvents.TryGetValue(id, out var profEvent))
            {
                profEvent.End = timestamp;
                return true;
            }

            return false;
        }

        public ProfileEventCollection Build()
        {
            foreach (var (id, evt) in _profileEvents)
            {
                if (evt.Parent != null)
                    continue;

                if (_profileEvents.TryGetValue(evt.ParentId, out var parent))
                {
                    evt.Parent = parent;
                }
            }

            var items = new ProfileEventCollection(_profileEvents.Values);
            return items;
        }
    }

    class ThreadSummaryData
    {
        public int Id { get; set; }

        public long EventCount { get; set; }
    }

    enum ProfileEventType
    {
        StartMethod,
        EndMethod
    }

    class ProfileEvent
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

        public ProfileEvent Parent { get; set; }
    }
}
