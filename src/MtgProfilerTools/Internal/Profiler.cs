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
        private static volatile bool IsEnabled = false;

        private static EventWriterThread WriterThread;

        private static long EventId = 0;

        [ThreadStatic]
        private static LinkedList<long> ActivityIds = new LinkedList<long>();

        static Profiler()
        {
            IsEnabled = SetupThread();
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

        /// <summary>
        /// Internally disable when we are broken
        /// </summary>
        internal static void Disable()
        {
            IsEnabled = false;
        }

        private static bool SetupThread()
        {
            string dataDir = Environment.GetEnvironmentVariable("MTGPROFILER_DATADIR");
            if (!string.IsNullOrEmpty(dataDir))
            {
                try
                {
                    string maxFileSizeSetting = Environment.GetEnvironmentVariable("MTGPROFILER_MAXFILESIZE");
                    long maxFileSize = 100;
                    if (string.IsNullOrEmpty(maxFileSizeSetting))
                    {
                        if (int.TryParse(maxFileSizeSetting, out int value))
                        {
                            maxFileSize = value;
                        }
                    }

                    if (maxFileSize <= 0)
                    {
                        return false;
                    }

                    // covert from MB
                    maxFileSize = maxFileSize * 1024 * 1024;

                    if (!Directory.Exists(dataDir))
                    {
                        Directory.CreateDirectory(dataDir);
                    }

                    var streamProvider = new OutputStreamProvider(dataDir);
                    WriterThread = new EventWriterThread(streamProvider, maxFileSize);
                    WriterThread.Run();
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
