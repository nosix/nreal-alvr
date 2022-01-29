using System;
using UnityEngine;

namespace Alvr
{
    [Serializable]
    public class SafeAngle
    {
        public Vector3 min;
        public Vector3 max;

        private Vector3 _adjustment;
        private Vector3 _threshold;

        /// <summary>
        /// Must be called before calling Contains
        /// </summary>
        public void Reset()
        {
            var origin = (min + max) / 2f;
            _adjustment = origin - (360f + 180f) * Vector3.one;
            _threshold = max - origin;
        }

        /// <summary>
        /// Returns true if the angle is within the range of min and max
        /// </summary>
        public bool Contains(Vector3 angle)
        {
            // The middle between min and max becomes 0
            angle -= _adjustment;
            var x = angle.x % 360f - 180f;
            var y = angle.y % 360f - 180f;
            var z = angle.z % 360f - 180f;
            return Mathf.Abs(x) < _threshold.x && Mathf.Abs(y) < _threshold.y && Mathf.Abs(z) < _threshold.z;
        }

        /// <summary>
        /// Get symmetric values in the y-z plane
        /// </summary>
        public SafeAngle Mirror()
        {
            return new SafeAngle
            {
                min = new Vector3(min.x, -(max.y - 360f), -(max.z - 360f)),
                max = new Vector3(max.x, -(min.y - 360f), -(min.z - 360f))
            };
        }
    }
}