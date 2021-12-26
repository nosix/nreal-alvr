using UnityEngine;

namespace Alvr
{
    public class TrackingDisable : MonoBehaviour
    {
        private void Awake()
        {
            DeviceDataManager.TrackingProducer += GetTracking;
        }

        private static Tracking GetTracking()
        {
            return new Tracking();
        }

        private void OnDestroy()
        {
            DeviceDataManager.TrackingProducer -= GetTracking;
        }
    }
}