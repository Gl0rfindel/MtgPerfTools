using System;
using System.Collections.Generic;

namespace MtgProfileAnalyzer
{
    internal class ProfileEventCollectionBuilder
    {
        private readonly List<ProfileEvent> _profileEvents;
        private readonly Dictionary<long, string> _eventNames;
        private long _minTs;

        public ProfileEventCollectionBuilder(SessionData session)
        {
            _profileEvents = new List<ProfileEvent>();
            _eventNames = new Dictionary<long, string>();
            Session = session;
            _minTs = long.MaxValue;
        }

        public SessionData Session { get; }

        public void Add(ProfileEvent @event)
        {
            _profileEvents.Add(@event);
            if (!string.IsNullOrEmpty(@event.Name))
            {
                _eventNames[@event.Id] = @event.Name;
            }

            if (@event.RawTimestamp < _minTs)
            {
                _minTs = @event.RawTimestamp;
            }
        }

        public ProfileEventCollection Build()
        {
            var calc = new StopwatchCalculator(Session.StopwatchFrequency);
            var actualMin = Math.Min(Session.InitialTimestamp, _minTs);

            foreach (var evt in _profileEvents)
            {
                evt.Offset = calc.ToTimeSpan(actualMin, evt.RawTimestamp);
                if (string.IsNullOrEmpty(evt.Name))
                {
                    if (_eventNames.TryGetValue(evt.Id, out string name))
                    {
                        evt.Name = name;
                    }
                }
            }

            var items = new ProfileEventCollection(Session, _profileEvents);
            return items;
        }
    }
}
