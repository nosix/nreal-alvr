using UnityEngine;

namespace Alvr
{
    public class TrackingDisable : MonoBehaviour
    {
        private readonly Tracking _tracking = new Tracking();

        private void Awake()
        {
            DeviceAdapter.GetTrackingDelegate += GetTracking;
            DeviceAdapter.OnRenderedDelegate += OnRendered;
        }

        private Tracking GetTracking(long frameIndex)
        {
            return _tracking;
        }

        private static void OnRendered(long frameIndex)
        {
        }

        private void OnDestroy()
        {
            DeviceAdapter.GetTrackingDelegate -= GetTracking;
            DeviceAdapter.OnRenderedDelegate -= OnRendered;
        }
    }
}