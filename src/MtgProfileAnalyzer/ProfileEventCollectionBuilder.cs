using System.Collections.Generic;

namespace MtgProfileAnalyzer
{
    internal class ProfileEventCollectionBuilder
    {
        private readonly Dictionary<long, ProfileEvent> _profileEvents;
        private StopwatchCalculator _stopwatchCalc;

        public ProfileEventCollectionBuilder()
        {
            _profileEvents = new Dictionary<long, ProfileEvent>();
        }

        public long StopwatchFrequency
        {
            get => _stopwatchCalc?.Frequency ?? 0;
            set
            {
                if (value > 0)
                {
                    _stopwatchCalc = new StopwatchCalculator(value);
                }
                else
                {
                    _stopwatchCalc = null;
                }
            }
        }

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
                Start = timestamp
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

                if (_stopwatchCalc != null && evt.End > 0 && evt.Start > 0)
                {
                    evt.Duration = _stopwatchCalc.ToTimeSpan(evt.Start, evt.End);
                }
            }

            var items = new ProfileEventCollection(_profileEvents.Values);
            return items;
        }
    }
}
