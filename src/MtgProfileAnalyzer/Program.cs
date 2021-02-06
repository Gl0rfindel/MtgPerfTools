using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MtgProfileAnalyzer
{
    class Program
    {
        async static Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Specify an input file");
                return 1;
            }

            int exitCode = 0;
            foreach (var arg in args)
            {
                string filePath = arg;
                if (!File.Exists(filePath))
                {
                    exitCode = 1;
                    continue;
                }

                try
                {
                    await AnalyzeFile(filePath);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error processing file '{filePath}': {e}");
                    exitCode = 1;
                }
            }

            return exitCode;
        }

        private static async Task AnalyzeFile(string inputFilePath)
        {
            using var inputFs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(inputFs);
            var header = reader.ReadBytes(64);
            var mtg = header[0..4]; // TODO: Validate
            var version = header[4..8]; // TODO Validate
            var startDateTimeTicks = BitConverter.ToInt64(header[8..16]);
            var frequency = BitConverter.ToInt64(header[16..24]);
            var startTs = BitConverter.ToInt64(header[24..32]);

            var session = new SessionData()
            {
                File = inputFilePath,
                Timestamp = new DateTime(startDateTimeTicks),
                StopwatchFrequency = frequency,
                InitialTimestamp = startTs,
            };

            var builder = new ProfileEventCollectionBuilder(session);
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

                    var profileEvent = new ProfileEvent()
                    {
                        EventType = eventType,
                        ThreadId = threadId,
                        ParentId = parentId,
                        Id = id,
                        Name = name,
                        RawTimestamp = ts,
                    };

                    builder.Add(profileEvent);
                    records++;
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }

            Console.WriteLine($"Read {records} records.");

            var collection = builder.Build();
            var ssFormat = collection.ToSpeedscopeFormat();

            string outputFileName = ssFormat.Name ?? "data";
            outputFileName = $"{outputFileName}.speedscope.json";

            // note extension is a required part which is not in the spec
            string outputDir = Path.GetDirectoryName(Path.GetFullPath(inputFilePath));

            string outputFile = Path.Combine(outputDir, outputFileName);
            using var dataFile = File.Create(outputFile);
            await JsonSerializer.SerializeAsync(dataFile, ssFormat, new JsonSerializerOptions()
            {
                Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Console.WriteLine($"Wrote to {outputFileName}");
        }
    }
}
