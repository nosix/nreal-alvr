using UnityEngine;

namespace Alvr
{
    public class TrackingDisable : MonoBehaviour
    {
        private readonly Tracking _tracking = new Tracking();

        private void Awake()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }

        private Tracking GetTracking()
        {
            return _tracking;
        }

        private void OnDestroy()
        {
            DeviceDataManager.TrackingProducer -= GetTracking;
        }
    }
}