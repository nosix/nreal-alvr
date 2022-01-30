namespace Alvr
{
    public class LocalLevelModelKalmanFilter
    {
        private readonly float _sigmaW;
        private readonly float _sigmaV;
        private float _p;

        public float Value { get; private set; }

        /**
         * <param name="sigmaW">Sigma of process noise</param>
         * <param name="sigmaV">Sigma of observation noise</param>
         * <param name="p0">Initial variance estimate</param>
         * <param name="value0">Initial state estimate</param>
         */
        public LocalLevelModelKalmanFilter(float sigmaW, float sigmaV, float value0 = 0f, float p0 = 0f)
        {
            _sigmaW = sigmaW;
            _sigmaV = sigmaV;
            _p = p0;
            Value = value0;
        }

        public float Next(float observed)
        {
            _p += _sigmaW;

            var gain = _p / (_p + _sigmaV);
            Value += gain * (observed - Value);
            _p *= 1 - gain;

            return Value;
        }

        public void Update(ref float value)
        {
            value = Next(value);
        }
    }
}