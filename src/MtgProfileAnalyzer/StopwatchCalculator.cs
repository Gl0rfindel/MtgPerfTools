using System;

namespace MtgProfileAnalyzer
{
    internal class StopwatchCalculator
    {
        private double _tsToTicks;

        public StopwatchCalculator(long frequency)
        {
            Frequency = frequency;
            _tsToTicks = TimeSpan.TicksPerSecond / (double)frequency;
        }

        public long Frequency { get; }

        public TimeSpan ToTimeSpan(long start, long end)
        {
            long delta = end - start;
            long ticks = (long)(_tsToTicks * delta);
            return new TimeSpan(ticks);
        }

        public static TimeSpan ToTimeSpan(long start, long end, long frequency)
        {
            var tsToTicks = TimeSpan.TicksPerSecond / (double)frequency;
            long delta = end - start;
            long ticks = (long)(tsToTicks * delta);
            return new TimeSpan(ticks);
        }
    }
}
