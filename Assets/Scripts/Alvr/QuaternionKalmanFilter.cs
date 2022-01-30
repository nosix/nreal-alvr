using UnityEngine;

namespace Alvr
{
    public class QuaternionKalmanFilter
    {
        private readonly LocalLevelModelKalmanFilter _x;
        private readonly LocalLevelModelKalmanFilter _y;
        private readonly LocalLevelModelKalmanFilter _z;
        private readonly Vector3 _zeroTo;

        public QuaternionKalmanFilter(
            float sigmaW, float sigmaV, float value0 = 0f, float p0 = 0f, float zeroTo = 180f
        )
        {
            _x = new LocalLevelModelKalmanFilter(sigmaW, sigmaV, value0, p0);
            _y = new LocalLevelModelKalmanFilter(sigmaW, sigmaV, value0, p0);
            _z = new LocalLevelModelKalmanFilter(sigmaW, sigmaV, value0, p0);
            _zeroTo = zeroTo * Vector3.one;
        }

        private Quaternion Next(Quaternion observed)
        {
            // Adjust so that the changing value becomes a continuous value
            // For example, when changing up and down with 0 degree as a reference,
            // -10 degrees (350 degrees) is 170 degrees and 10 degrees is 190 degrees.
            var observedAngles = observed.eulerAngles + _zeroTo;
            var x = _x.Next(observedAngles.x % 360);
            var y = _y.Next(observedAngles.y % 360);
            var z = _z.Next(observedAngles.z % 360);
            return Quaternion.Euler(
                (x - _zeroTo.x + 360f) % 360f,
                (y - _zeroTo.y + 360f) % 360f,
                (z - _zeroTo.z + 360f) % 360f
            );
        }

        public void Update(ref Quaternion value)
        {
            value = Next(value);
        }
    }
}