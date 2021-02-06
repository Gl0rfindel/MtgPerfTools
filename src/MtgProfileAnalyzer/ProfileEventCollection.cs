using System;
using System.Collections.Generic;

namespace MtgProfileAnalyzer
{
    internal class ProfileEventCollection
    {
        private Dictionary<int, List<ProfileEvent>> _byThread;
        private List<ThreadSummaryData> _threadSummaries;

        public ProfileEventCollection(SessionData session, IEnumerable<ProfileEvent> events)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));

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

            foreach (var (_, list) in _byThread)
            {
                list.Sort((l, r) => l.RawTimestamp.CompareTo(r.RawTimestamp));
            }
        }

        public SessionData Session { get; }

        public IReadOnlyList<ThreadSummaryData> ThreadSummaries => _threadSummaries;

        public IEnumerable<ProfileEvent> GetEvents(int threadId)
        {
            return _byThread[threadId];
        }
    }
}
