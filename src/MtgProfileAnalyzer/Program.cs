using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace MtgProfileAnalyzer
{
    class Program
    {
        async static Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption();
            app.Command("analyze", cmd =>
            {
                var filesArg = cmd.Argument("files", "Raw profiling input files to process", true).IsRequired();

                cmd.OnExecuteAsync(async ct =>
                {
                    int exitCode = 0;
                    foreach (var filePath in filesArg.Values)
                    {
                        if (!File.Exists(filePath))
                        {
                            Console.Error.WriteLine($"File does not exist: {filePath}");
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
                });
            });

            app.Command("watch", cmd =>
            {
                var dirARg = cmd.Argument("directory", "A directory to watch for new files to process");

                cmd.OnExecuteAsync(async ct =>
                {
                    string watchDirectory = dirARg.Value ?? Environment.GetEnvironmentVariable("MTG_PROFILER_DATA_DIR");
                    if (string.IsNullOrEmpty(watchDirectory))
                    {
                        Console.Error.WriteLine("Watch dir was not specified or found in environment variable MTG_PROFILER_DATA_DIR");
                        return 1;
                    }
                    else if (!Directory.Exists(watchDirectory))
                    {
                        Console.Error.WriteLine($"Directory does not exist {watchDirectory}");
                        return 1;
                    }

                    Console.WriteLine($"Watching {watchDirectory} for changes...");
                    using var watch = new FileSystemWatcher(watchDirectory, "*.bin");

                    var pending = new Queue<(string filePath, int retry)>();
                    while (true)
                    {
                        var info = watch.WaitForChanged(WatcherChangeTypes.All, 2_000);
                        string inputFilePath;
                        int retry;
                        if (info.TimedOut)
                        {
                            if (!pending.TryDequeue(out var item))
                            {
                                continue;
                            }

                            inputFilePath = item.filePath;
                            retry = item.retry;
                        }
                        else
                        {
                            if (info.ChangeType == WatcherChangeTypes.Deleted)
                            {
                                continue;
                            }

                            inputFilePath = Path.Combine(watchDirectory, info.Name);
                            retry = 0;
                        }

                        try
                        {
                            using var fs = File.OpenRead(inputFilePath);
                        }
                        catch (Exception)
                        {
                            if (retry < 3)
                            {
                                pending.Enqueue((inputFilePath, retry + 1));
                            }

                            continue;
                        }

                        string name = Path.GetFileName(inputFilePath);
                        Console.WriteLine($"Analyzing {name}");
                        string outputFile = await AnalyzeFile(inputFilePath);
                    }
                });
            });

            return await app.ExecuteAsync(args);
        }

        private static async Task<string> AnalyzeFile(string inputFilePath)
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

            return outputFile;
        }
    }
}
