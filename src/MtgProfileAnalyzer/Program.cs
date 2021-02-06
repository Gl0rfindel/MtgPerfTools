using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MtgProfileAnalyzer
{
    class Program
    {
        async static Task Main(string[] args)
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

            using var inputFs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(inputFs);
            var header = reader.ReadBytes(64);
            var mtg = header[0..4];
            var version = header[4..8];
            var startDateTimeTicks = BitConverter.ToInt64(header[8..16]);
            var frequency = BitConverter.ToInt64(header[16..24]);
            var startTs = BitConverter.ToInt64(header[24..32]);

            var builder = new ProfileEventCollectionBuilder();
            builder.StopwatchFrequency = frequency;
            int records = 0;
            int startRecs = 0;
            int endRecs = 0;
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
                        startRecs++;
                    }
                    else if (eventType == ProfileEventType.EndMethod)
                    {
                        builder.SetEnd(ts, id);
                        endRecs++;
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

            Console.WriteLine($"Read {records} records.");
            if (startRecs != endRecs)
            {
                Console.WriteLine($"Start: {startRecs}, End: {endRecs}");
            }

            var collection = builder.Build();
            var threadData = collection.GetThreadData();
            ThreadSummaryData max = null;
            foreach (var thread in threadData)
            {
                if (max == null || thread.EventCount > max.EventCount)
                {
                    max = thread;
                }

                Console.WriteLine($"ThreadId: {thread.Id}, Events: {thread.EventCount}");
            }

            if (max != null)
            {
                var thread = max;
                Console.WriteLine($"Using {thread.Id} since it has the most events");

                var root = new D3FlameObject()
                {
                    Name = "root",
                };

                var map = new Dictionary<ProfileEvent, D3FlameObject>();
                long finalEnd = long.MinValue;
                foreach (var evt in collection.GetEvents(thread.Id))
                {
                    var flame = new D3FlameObject()
                    {
                        Name = evt.Name,
                        Value = evt.Duration.Ticks,
                    };

                    map.Add(evt, flame);

                    if (evt.End > finalEnd)
                    {
                        finalEnd = evt.End;
                    }
                }
                
                foreach (var (evt, flame) in map)
                {
                    if (evt.Parent == null)
                    {
                        root.Children.Add(flame);
                    }
                    else
                    {
                        var parentFlame = map[evt.Parent];
                        parentFlame.Children.Add(flame);
                    }
                }

                root.Value = StopwatchCalculator.ToTimeSpan(startTs, finalEnd, frequency).Ticks;

                string dataJsonPath = Path.Combine(AppContext.BaseDirectory, "data.json");
                using var dataFile = File.Create(dataJsonPath);
                await JsonSerializer.SerializeAsync(dataFile, root, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                Console.WriteLine($"Wrote to {dataJsonPath}");
            }
        }
    }
}
