namespace Alvr
{
    public class DataSampleFilter
    {
        private readonly IntervalTimeRecorder _interval;
        private readonly float _windowMs;
        private readonly int _windowSamples;
        private int _count;

        /**
         * <param name="interval">Interval when samples are taken (Take is called)</param>
         * <param name="windowMs">Window period (in Milliseconds)</param>
         * <param name="windowsSamples">Number of samples per window</param>
         */
        public DataSampleFilter(IntervalTimeRecorder interval, float windowMs, int windowsSamples)
        {
            _interval = interval;
            _windowMs = windowMs;
            _windowSamples = windowsSamples;
        }

        public bool Take()
        {
            var frequency = _windowMs / _interval.Value;
            var step = (int)(frequency / _windowSamples);

            // Take a sample only once in step times
            if (step <= 1) return true;
            _count = (_count + 1) % step;
            return _count == 0;
        }
    }
}