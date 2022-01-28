using System;
using UnityEngine;

namespace Alvr
{
    [Serializable]
    public class EyeFov
    {
        public float diagonalFovAngle = 52f;
        public float fovRatioInner = 45f;
        public float fovRatioOuter = 49f;
        public float fovRatioUpper = 50f;
        public float fovRatioLower = 48f;
        public float zoomRatio = 1f;

        public CRect[] Get(float width, float height)
        {
            var lFov = GetLEyeFov(width, height);
            var rFov = GetREyeFov(lFov);
            return new[] { lFov, rFov };
        }

        private CRect GetLEyeFov(float width, float height)
        {
            var screenDiagonalAngleFromAdjacent = Mathf.Atan(height / width);
            var screenWidthAngle = Mathf.Cos(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            var screenHeightAngle = Mathf.Sin(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            var hDenominator = fovRatioInner + fovRatioOuter;
            var vDenominator = fovRatioUpper + fovRatioLower;
            return new CRect
            {
                left = screenWidthAngle * (fovRatioOuter / hDenominator) / zoomRatio,
                right = screenWidthAngle * (fovRatioInner / hDenominator) / zoomRatio,
                top = screenHeightAngle * (fovRatioUpper / vDenominator) / zoomRatio,
                bottom = screenHeightAngle * (fovRatioLower / vDenominator) / zoomRatio
            };
        }

        private static CRect GetREyeFov(CRect leftEyeFov)
        {
            return new CRect
            {
                left = leftEyeFov.right,
                right = leftEyeFov.left,
                top = leftEyeFov.top,
                bottom = leftEyeFov.bottom
            };
        }
    }
}