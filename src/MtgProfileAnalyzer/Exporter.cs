using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MtgProfileAnalyzer.Export.Speedscope;

namespace MtgProfileAnalyzer
{
    internal static class Exporter
    {
        public static SpeedscopeFile ToSpeedscopeFormat(this ProfileEventCollection collection)
        {
            var session = collection.Session;
            var file = new SpeedscopeFile();
            if (session.File != null)
            {
                file.Name = Path.GetFileNameWithoutExtension(session.File);
            }

            foreach (var threadData in collection.ThreadSummaries)
            {
                string name = collection.ThreadSummaries.Count == 1 ? "Main Thread" : $"Thread {threadData.Id}";

                var profileEvents = collection.GetEvents(threadData.Id);
                var profile = new EventedProfile()
                {
                    Unit = ValueUnit.Milliseconds,
                    StartValue = 0.0,
                    Name = name
                };

                file.Profiles.Add(profile);

                var frameLookup = new Dictionary<string, (int index, Frame frame)>();
                double maxValue = -1;
                foreach (var profileEvent in profileEvents)
                {
                    if (!frameLookup.TryGetValue(profileEvent.Name, out var frameInfo))
                    {
                        var frame = new Frame()
                        {
                            Name = profileEvent.Name
                        };

                        int index = file.Shared.Frames.Count;
                        file.Shared.Frames.Add(frame);

                        frameInfo = (index, frame);
                        frameLookup.Add(profileEvent.Name, frameInfo);
                    }

                    IEvent ssEvent = profileEvent.EventType switch
                    {
                        ProfileEventType.StartMethod => new OpenEvent() { At = profileEvent.Offset.TotalMilliseconds, Frame = frameInfo.index },
                        ProfileEventType.EndMethod => new CloseEvent() { At = profileEvent.Offset.TotalMilliseconds, Frame = frameInfo.index },
                        _ => throw new ArgumentException("Invalid event type"),
                    };

                    profile.Events.Add(ssEvent);

                    if (ssEvent.At > maxValue)
                    {
                        maxValue = ssEvent.At;
                    }
                }

                profile.EndValue = maxValue;
            }

            return file;
        }
    }
}
