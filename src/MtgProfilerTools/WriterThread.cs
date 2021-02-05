using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MtgProfilerTools
{
    internal sealed class WriterThread
    {
        private readonly object _lock;
        private readonly Queue<RawProfileEvent> _entries = new Queue<RawProfileEvent>();
        private readonly Thread _thread;
        private readonly Stream _outputStream;

        public WriterThread(Stream outputStream)
        {
            _lock = new object();
            _thread = new Thread(new ParameterizedThreadStart(Run))
            {
                Name = "MtgProfilerWriterThread",
                IsBackground = true,
            };

            _outputStream = outputStream;
        }

        public void Run()
        {
            var data = new ThreadData()
            {
                Lock = _lock,
                Queue = _entries,
                OutputStream = _outputStream
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
            var output = threadData.OutputStream;
            WriteDataHeader(output);
            output.Flush();
            var writer = new BinaryWriter(output, Encoding.UTF8);
            while (true)
            {
                lock (locker)
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
                        }
                        catch { }
                    }
                }
            }
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

        private class ThreadData
        {
            public object Lock;

            public Queue<RawProfileEvent> Queue;

            public Stream OutputStream;
        }
    }
}
