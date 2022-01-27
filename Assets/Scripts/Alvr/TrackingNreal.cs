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
        [SerializeField] private HandTracking handTracking;
        [SerializeField] private UnityEvent<Pose, Pose> onRendered;

        private const float DiagonalFovAngle = 52f;
        private const int CoefficientOfLHand = 1;
        private const int CoefficientOfRHand = -1;

        private readonly Tracking _tracking = new Tracking();
        private readonly HeadPoseHistory _headPoseHistory = new HeadPoseHistory();

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
            _tracking.headPoseOrientation = new CQuaternion
            {
                x = headPose.rotation.x,
                y = headPose.rotation.y,
                z = headPose.rotation.z,
                w = headPose.rotation.w
            };
            if (handTracking != null)
            {
                handTracking.UpdateHandState();
                var lCtrlState = handTracking.LCtrlState;
                var rCtrlState = handTracking.RCtrlState;
                var lOrientation = ConvertHandAxis(lCtrlState.Orientation, CoefficientOfLHand);
                var rOrientation = ConvertHandAxis(rCtrlState.Orientation, CoefficientOfRHand);
                _tracking.lCtrl = new Controller
                {
                    buttons = lCtrlState.Buttons,
                    trackpadPositionX = lCtrlState.Input2DPosition.x,
                    trackpadPositionY = lCtrlState.Input2DPosition.y,
                    triggerValue = lCtrlState.Trigger,
                    gripValue = lCtrlState.Grip,
                    orientation = new CQuaternion
                    {
                        x = lOrientation.x,
                        y = lOrientation.y,
                        z = lOrientation.z,
                        w = lOrientation.w
                    },
                    position = new CVector3
                    {
                        x = lCtrlState.Position.x + _tracking.headPosePosition.x,
                        y = lCtrlState.Position.y + _tracking.headPosePosition.y + 0.15f,
                        z = lCtrlState.Position.z + _tracking.headPosePosition.z
                    }
                };
                _tracking.rCtrl = new Controller
                {
                    buttons = rCtrlState.Buttons,
                    trackpadPositionX = rCtrlState.Input2DPosition.x,
                    trackpadPositionY = rCtrlState.Input2DPosition.y,
                    triggerValue = rCtrlState.Trigger,
                    gripValue = rCtrlState.Grip,
                    orientation = new CQuaternion
                    {
                        x = rOrientation.x,
                        y = rOrientation.y,
                        z = rOrientation.z,
                        w = rOrientation.w
                    },
                    position = new CVector3
                    {
                        x = rCtrlState.Position.x + _tracking.headPosePosition.x,
                        y = rCtrlState.Position.y + _tracking.headPosePosition.y + 0.15f,
                        z = rCtrlState.Position.z + _tracking.headPosePosition.z
                    }
                };
            }

            _headPoseHistory.Add(frameIndex, headPose);
            return _tracking;
        }

        private static Pose GetHeadPose()
        {
            return new Pose(NRFrame.HeadPose.position.ToAlvr(), NRFrame.HeadPose.rotation.ToAlvr());
        }

        private static Quaternion ConvertHandAxis(Quaternion rotation, int coefficientOfHand)
        {
            return rotation * Quaternion.Euler(90, coefficientOfHand * 90, 0);
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