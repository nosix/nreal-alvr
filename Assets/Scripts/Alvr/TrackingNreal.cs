using NRKernal;
using UnityEngine;

namespace Alvr
{
    public class TrackingNreal : MonoBehaviour
    {
        [SerializeField] private AlvrClient alvrClient;

        private const float DiagonalFovAngle = 52f;

        private readonly Tracking _tracking = new Tracking();

        private static Rect GetEyeFov(float diagonalFovAngle, float width, float height)
        {
            var screenDiagonalAngleFromAdjacent = Mathf.Atan(height / width);
            var screenWidthAngle = Mathf.Cos(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            var screenHeightAngle = Mathf.Sin(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            return new Rect
            {
                left = screenWidthAngle,
                right = screenWidthAngle,
                top = screenHeightAngle,
                bottom = screenHeightAngle
            };
        }

        private void Awake()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }

        private Tracking GetTracking()
        {
            var eyeFov = GetEyeFov(DiagonalFovAngle, alvrClient.EyeWidth, alvrClient.EyeHeight);
            var headPosePosition = NRFrame.HeadPose.position;
            var headPoseRotation = UnityEngine.Quaternion.Inverse(NRFrame.HeadPose.rotation);
            _tracking.ipd = 0.068606f;
            _tracking.battery = 100;
            _tracking.plugged = 1;
            _tracking.lEyeFov = eyeFov;
            _tracking.rEyeFov = eyeFov;
            _tracking.headPosePosition = new Vector3
            {
                x = headPosePosition.x,
                y = headPosePosition.y,
                z = headPosePosition.z
            };
            _tracking.headPoseOrientation = new Quaternion
            {
                x = headPoseRotation.x,
                y = headPoseRotation.y,
                z = headPoseRotation.z,
                w = headPoseRotation.w
            };
            return _tracking;
        }

        private void OnDestroy()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }
    }
}