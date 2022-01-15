using UnityEngine;

namespace Alvr
{
    public static class NrExtension
    {
        private static readonly Vector3 CoordinatesSystem = new Vector3(1f, 1f, -1f);
        private static readonly Vector3 RotateDirection = -CoordinatesSystem;

        public static Quaternion ToAlvr(this Quaternion rotation)
        {
            return Quaternion.Euler(
                Vector3.Scale(rotation.eulerAngles, RotateDirection)
            );
        }

        public static Vector3 ToAlvr(this Vector3 position)
        {
            return Vector3.Scale(position, CoordinatesSystem);
        }
    }
}