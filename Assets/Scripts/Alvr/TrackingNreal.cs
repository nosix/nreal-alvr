using NRKernal;
using UnityEngine;
using UnityEngine.Events;

namespace Alvr
{
    public class TrackingNreal : MonoBehaviour
    {
        [SerializeField] private AlvrClient alvrClient;
        [SerializeField] private float eyeHeight = 0.66f;
        [SerializeField] private float fovRatioInner = 45f;
        [SerializeField] private float fovRatioOuter = 49f;
        [SerializeField] private float fovRatioUpper = 50f;
        [SerializeField] private float fovRatioLower = 48f;
        [SerializeField] private float zoomRatio = 1f;
        [SerializeField] private float handUpwardMovement = 0.2f;
        [SerializeField] private float handForwardMovement = 0.5f;
        [SerializeField] private HandTracking handTracking;
        [SerializeField] private UnityEvent<Pose, Pose> onRendered;

        private const float DiagonalFovAngle = 52f;

        private readonly Tracking _tracking = new Tracking();
        private readonly HeadPoseHistory _headPoseHistory = new HeadPoseHistory();

        private Vector3 HandUpwardMovement => Vector3.up.ToAlvr() * (eyeHeight + handUpwardMovement);
        private Vector3 HandForwardMovement => Vector3.forward.ToAlvr() * handForwardMovement;

        private CRect GetLEyeFov(float diagonalFovAngle, float width, float height)
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

        private void Awake()
        {
            DeviceAdapter.GetTrackingDelegate += GetTracking;
            DeviceAdapter.OnRenderedDelegate += OnRendered;
        }

        private Tracking GetTracking(long frameIndex)
        {
            var lEyeFov = GetLEyeFov(DiagonalFovAngle, alvrClient.EyeWidth, alvrClient.EyeHeight);
            var rEyeFov = GetREyeFov(lEyeFov);
            var headPose = GetHeadPose();
            _tracking.ipd = 0.068606f;
            _tracking.battery = 100;
            _tracking.plugged = 1;
            _tracking.lEyeFov = lEyeFov;
            _tracking.rEyeFov = rEyeFov;
            _tracking.headPosePosition = new CVector3
            {
                x = headPose.position.x,
                y = headPose.position.y + eyeHeight,
                z = headPose.position.z
            };
            _tracking.headPoseOrientation = headPose.rotation.ToCStruct();
            if (handTracking != null)
            {
                handTracking.UpdateHandState();
                var lCtrlState = handTracking.LCtrlState;
                var rCtrlState = handTracking.RCtrlState;
                var handOrigin = headPose.position + HandUpwardMovement + headPose.rotation * HandForwardMovement;
                _tracking.lCtrl = new Controller
                {
                    buttons = lCtrlState.Buttons,
                    trackpadPositionX = lCtrlState.Input2DPosition.x,
                    trackpadPositionY = lCtrlState.Input2DPosition.y,
                    triggerValue = lCtrlState.Trigger,
                    gripValue = lCtrlState.Grip,
                    orientation = lCtrlState.Orientation.ToCStruct(),
                    position = (lCtrlState.Position + handOrigin).ToCStruct()
                };
                _tracking.rCtrl = new Controller
                {
                    buttons = rCtrlState.Buttons,
                    trackpadPositionX = rCtrlState.Input2DPosition.x,
                    trackpadPositionY = rCtrlState.Input2DPosition.y,
                    triggerValue = rCtrlState.Trigger,
                    gripValue = rCtrlState.Grip,
                    orientation = rCtrlState.Orientation.ToCStruct(),
                    position = (rCtrlState.Position + handOrigin).ToCStruct()
                };
            }

            _headPoseHistory.Add(frameIndex, headPose);
            return _tracking;
        }

        private static Pose GetHeadPose()
        {
            return new Pose(NRFrame.HeadPose.position.ToAlvr(), NRFrame.HeadPose.rotation.ToAlvr());
        }

        private void OnRendered(long frameIndex)
        {
            if (_headPoseHistory.Has(frameIndex))
            {
                onRendered.Invoke(_headPoseHistory.Get(frameIndex), GetHeadPose());
            }
        }

        private void OnDestroy()
        {
            DeviceAdapter.GetTrackingDelegate -= GetTracking;
            DeviceAdapter.OnRenderedDelegate -= OnRendered;
        }
    }
}