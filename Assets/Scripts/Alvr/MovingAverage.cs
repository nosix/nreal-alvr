using System.Collections.Generic;
using JetBrains.Annotations;

namespace Alvr
{
    /**
     * Calculate the moving average
     */
    public class MovingAverage
    {
        private readonly DataSampleFilter _filter;
        private readonly Queue<float> _samples;
        private readonly int _n;
        private float _total;

        /**
         * <param name="n">Number of samples</param>
         * <param name="filter">Filter to adjust the interval to get samples</param>
         */
        public MovingAverage(int n, [CanBeNull] DataSampleFilter filter = null)
        {
            _n = n;
            _samples = new Queue<float>(n);
            _filter = filter;
        }

        public float Average => _samples.Count == 0 ? 0 : _total / _samples.Count;

        public void Next(float value)
        {
            if (_filter != null && !_filter.Take()) return;

            if (_samples.Count == _n)
            {
                _total -= _samples.Dequeue();
            }
            _samples.Enqueue(value);
            _total += value;
        }
    }
}