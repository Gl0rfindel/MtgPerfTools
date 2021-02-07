using System;
using System.Collections.Generic;
using System.IO;
using MtgProfileAnalyzer.Export.Speedscope;

namespace MtgProfileAnalyzer
{
    internal static class Exporter
    {
        public static (SpeedscopeFile, ProcessingSummary) ToSpeedscopeFormat(this ProfileEventCollection collection)
        {
            var session = collection.Session;
            var file = new SpeedscopeFile();
            if (session.File != null)
            {
                file.Name = Path.GetFileNameWithoutExtension(session.File);
            }

            var summary = new ProcessingSummary();

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

                var openEvents = new Dictionary<long, (int index, ProfileEvent)>();
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

                    IEvent ssEvent;
                    if (profileEvent.EventType == ProfileEventType.StartMethod)
                    {
                        ssEvent = new OpenEvent() { At = profileEvent.Offset.TotalMilliseconds, Frame = frameInfo.index };
                        openEvents.Add(profileEvent.Id, (frameInfo.index, profileEvent));
                    }
                    else if (profileEvent.EventType == ProfileEventType.EndMethod)
                    {
                        ssEvent = new CloseEvent() { At = profileEvent.Offset.TotalMilliseconds, Frame = frameInfo.index };
                        openEvents.Remove(profileEvent.Id);
                    }
                    else
                    {
                        summary.DroppedEvents++;
                        continue;
                    }

                    profile.Events.Add(ssEvent);

                    if (ssEvent.At > maxValue)
                    {
                        maxValue = ssEvent.At;
                    }
                }

                profile.EndValue = maxValue;

                if (openEvents.Count > 0)
                {
                    summary.FixedUnbalancedOpenEvents += openEvents.Count;
                    foreach (var (id, (index, frameInfo)) in openEvents)
                    {
                        var fakeCloseEvent = new CloseEvent() 
                        { 
                            At = maxValue, 
                            Frame = index 
                        };

                        profile.Events.Add(fakeCloseEvent);
                    }
                }
            }

            return (file, summary);
        }
    }
}
