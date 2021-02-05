using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MtgProfilerTools
{
    internal sealed class WriterThread
    {
        private readonly object _lock;
        private readonly Queue<ProfileEvent> _entries = new Queue<ProfileEvent>();
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

        public void Enqueue(ProfileEvent profileEvent)
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
            var buffer = new byte[2048];
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
                            int dataCount = entry.ToBytes(buffer, 0);
                            output.Write(buffer, 0, dataCount);
                        }
                        catch { }
                    }
                }
            }
        }

        private class ThreadData
        {
            public object Lock;

            public Queue<ProfileEvent> Queue;

            public Stream OutputStream;
        }
    }
}
