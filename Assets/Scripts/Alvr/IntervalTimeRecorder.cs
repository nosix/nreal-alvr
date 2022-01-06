using System;

namespace Alvr
{
    /**
     * Records the NextTick call interval and returns the average value in milliseconds
     */
    public class IntervalTimeRecorder
    {
        private readonly MovingAverage _interval;
        private long _prevTick;

        public IntervalTimeRecorder(int samples)
        {
            _interval = new MovingAverage(samples);
            _prevTick = DateTime.Now.Ticks;
        }

        public float Value => _interval.Average;

        public void NextTick()
        {
            var currTick = DateTime.Now.Ticks;
            var diffTick = currTick - _prevTick;
            _prevTick = currTick;

            var intervalTimeMs = diffTick / TimeSpan.TicksPerMillisecond;
            _interval.Next(intervalTimeMs);
        }
    }
}