using NRKernal;
using UnityEngine;

namespace Alvr
{
    public class TrackingNreal : MonoBehaviour
    {
        private const float FOVAngle = 52f;

        private readonly Rect _eyeFov = new Rect
        {
            left = FOVAngle,
            right = FOVAngle,
            top = FOVAngle,
            bottom = FOVAngle
        };

        private void Awake()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }

        private Tracking GetTracking()
        {

            var headPosePosition = NRFrame.HeadPose.position;
            var headPoseRotation = UnityEngine.Quaternion.Inverse(NRFrame.HeadPose.rotation);
            return new Tracking
            {
                ipd = 0.068606f,
                battery = 100,
                plugged = 1,
                lEyeFov = _eyeFov,
                rEyeFov = _eyeFov,
                headPosePosition = new Vector3
                {
                    x = headPosePosition.x,
                    y = headPosePosition.y,
                    z = headPosePosition.z
                },
                headPoseOrientation = new Quaternion
                {
                    x = headPoseRotation.x,
                    y = headPoseRotation.y,
                    z = headPoseRotation.z,
                    w = headPoseRotation.w
                }
            };
        }

        private void OnDestroy()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }
    }
}