using UnityEngine;

namespace Alvr
{
    public class TrackingDisable : MonoBehaviour
    {
        [SerializeField] private AlvrClient alvrClient;

        [SerializeField] private float ipd = 0.068606f;
        [SerializeField] private EyeFov eyeFov;

        private readonly Tracking _tracking = new Tracking();

        private void Awake()
        {
            DeviceAdapter.GetTrackingDelegate += GetTracking;
            DeviceAdapter.OnRenderedDelegate += OnRendered;

            var eyeFovRects = eyeFov.Get(alvrClient.EyeWidth, alvrClient.EyeHeight);
            _tracking.ipd = ipd;
            _tracking.lEyeFov = eyeFovRects[0];
            _tracking.rEyeFov = eyeFovRects[1];
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