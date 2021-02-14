using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MtgProfilerTools
{
    internal sealed class EventWriterThread
    {
        private readonly object _lock;
        private readonly Queue<RawProfileEvent> _entries = new Queue<RawProfileEvent>();
        private readonly Thread _thread;
        private readonly OutputStreamProvider _streamProvider;
        private readonly long _maxFileSize;

        public EventWriterThread(OutputStreamProvider streamProvider, long maxFileSize)
        {
            _lock = new object();
            _thread = new Thread(new ParameterizedThreadStart(Run))
            {
                Name = $"MtgProfiler{nameof(EventWriterThread)}",
                IsBackground = true,
            };

            _streamProvider = streamProvider;
            _maxFileSize = maxFileSize;
        }

        public void Run()
        {
            var data = new ThreadData()
            {
                Lock = _lock,
                Queue = _entries,
                StreamProvider = _streamProvider,
                MaxFileSize = _maxFileSize
            };

            _thread.Start(data);
        }

        public void Enqueue(in RawProfileEvent profileEvent)
        {
            lock (_lock)
            {
                _entries.Enqueue(profileEvent);
                Monitor.Pulse(_lock);
            }
        }

        private static void Run(object state)
        {
            ThreadData threadData = (ThreadData)state;
            object locker = threadData.Lock;
            var queue = threadData.Queue;
            var streamProvider = threadData.StreamProvider;

            Stream currentStream = null;
            BinaryWriter writer = null;

            try
            {
                currentStream = streamProvider.OpenNewStream();
                writer = InitializeWriter(currentStream);

                lock (locker)
                {
                    while (true)
                    {
                        while (queue.Count == 0)
                        {
                            Monitor.Wait(locker);
                        }

                        while (queue.Count > 0)
                        {
                            try
                            {
                                var entry = queue.Dequeue();

                                writer.Write(entry.ThreadId);
                                writer.Write(entry.ParentId);
                                writer.Write(entry.ActivityId);
                                writer.Write(entry.Timestamp);
                                writer.Write((int)entry.EventType);
                                writer.Write(entry.Name);

                                writer.Flush();

                                if (currentStream.Length > threadData.MaxFileSize)
                                {
                                    streamProvider.WriteError("Exceeded max file size");
                                    writer.Close();
                                    currentStream.Close();
                                    Cleanup(currentStream);

                                    try
                                    {
                                        currentStream = streamProvider.OpenNewStream();
                                        writer = InitializeWriter(currentStream);
                                    }
                                    catch (Exception e)
                                    {
                                        streamProvider.WriteError($"Error opening new stream: {e}");
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            finally
            {
                Internal.Profiler.Disable();
                try
                {
                    if (writer != null)
                        writer.Close();

                    if (currentStream != null)
                        currentStream.Close();
                }
                catch { }
            }
        }

        private static BinaryWriter InitializeWriter(Stream outputStream)
        {
            WriteDataHeader(outputStream);
            outputStream.Flush();
            var writer = new BinaryWriter(outputStream, Encoding.UTF8);
            return writer;
        }

        private static void WriteDataHeader(Stream stream)
        {
            const int HeaderSize = 64;
            const int Version = 1;
            var header = new byte[HeaderSize];
            Encoding.UTF8.GetBytes("MTG!", 0, 4, header, 0);
            int offset = 4;
            offset = BitOperations.ToBytes(Version, header, offset);
            offset = BitOperations.ToBytes(DateTime.UtcNow.Ticks, header, offset);
            offset = BitOperations.ToBytes(Stopwatch.Frequency, header, offset);
            offset = BitOperations.ToBytes(Stopwatch.GetTimestamp(), header, offset);
            stream.Write(header, 0, header.Length);
        }

        private static void Cleanup(Stream stream)
        {
            if (stream is FileStream fs)
            {
                try
                {
                    File.Delete(fs.Name);
                }
                catch { }
            }
        }

        private class ThreadData
        {
            public object Lock;

            public Queue<RawProfileEvent> Queue;

            public OutputStreamProvider StreamProvider;

            public long MaxFileSize;
        }
    }
}
