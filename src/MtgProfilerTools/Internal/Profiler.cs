using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MtgProfilerTools.Internal
{
    /// <summary>
    /// These are methods the are injected into instrumented DLLs so don't change them.
    /// </summary>
    public class Profiler
    {
        private static bool IsEnabled = false;

        private static Stream OutpuStream;

        private static WriterThread WriterThread;

        private static long EventId = 0;

        [ThreadStatic]
        private static LinkedList<long> ActivityIds = new LinkedList<long>();

        static Profiler()
        {
            IsEnabled = false;
            string dataDir = Environment.GetEnvironmentVariable("MTG_PROFILER_DATA_DIR");
            if (!string.IsNullOrEmpty(dataDir))
            {
                try
                {
                    if (!Directory.Exists(dataDir))
                    {
                        Directory.CreateDirectory(dataDir);
                    }

                    string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"mtg_profile_data_{ts}.bin";
                    string outputPath = Path.Combine(dataDir, fileName);
                    OutpuStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
                    WriterThread = new WriterThread(OutpuStream);
                    WriterThread.Run();
                    IsEnabled = true;
                }
                catch { }
            }
        }

        public static void Enter(string name)
        {
            if (!IsEnabled)
                return;

            long parentId = ActivityIds.Count > 0 ? ActivityIds.Last.Value : -1L;
            long id = Interlocked.Increment(ref EventId);
            ActivityIds.AddLast(id);
            var profileEvent = new RawProfileEvent(Thread.CurrentThread.ManagedThreadId, parentId, id, Stopwatch.GetTimestamp(), ProfileEventType.StartMethod, name);
            WriterThread.Enqueue(profileEvent);
        }

        public static void Exit()
        {
            if (!IsEnabled)
                return;

            long id = -1L;
            long parentId = -1L;
            if (ActivityIds.Count > 0)
            {
                id = ActivityIds.Last.Value;
                ActivityIds.RemoveLast();
                if (ActivityIds.Count > 0)
                {
                    parentId = ActivityIds.Last.Value;
                }
            }

            var profileEvent = new RawProfileEvent(Thread.CurrentThread.ManagedThreadId, parentId, id, Stopwatch.GetTimestamp(), ProfileEventType.EndMethod, string.Empty);
            WriterThread.Enqueue(profileEvent);
        }
    }
}
