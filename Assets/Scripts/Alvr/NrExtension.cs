using UnityEngine;

namespace Alvr
{
    public static class NrExtension
    {
        private static readonly Vector3 RotateDirection = new Vector3(-1f, -1f, 1f);

        public static Quaternion ToAlvr(this Quaternion rotation)
        {
            return Quaternion.Euler(
                Vector3.Scale(rotation.eulerAngles, RotateDirection)
            );
        }
    }
}