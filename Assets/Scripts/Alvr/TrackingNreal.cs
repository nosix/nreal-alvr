using NRKernal;
using UnityEngine;
using UnityEngine.Events;

namespace Alvr
{
    public class TrackingNreal : MonoBehaviour, ITrackingSettingsTarget
    {
        [SerializeField] private AlvrClient alvrClient;
        [SerializeField] private float spaceScale = 0.5f;
        [SerializeField] private float eyeHeight = 0.66f;
        [SerializeField] private float ipd = 0.068606f;
        [SerializeField] private float diagonalFovAngle = 52f;
        [SerializeField] private float fovRatioInner = 45f;
        [SerializeField] private float fovRatioOuter = 49f;
        [SerializeField] private float fovRatioUpper = 50f;
        [SerializeField] private float fovRatioLower = 48f;
        [SerializeField] private float zoomRatio = 1f;
        [SerializeField] private float handUpwardMovement = 0.1f;
        [SerializeField] private float handForwardMovement = 0.1f;
        [SerializeField] private HandTracking handTracking;
        [SerializeField] private UnityEvent<Pose, Pose> onRendered;

        private readonly Tracking _tracking = new Tracking();
        private readonly HeadPoseHistory _headPoseHistory = new HeadPoseHistory();

        private Vector3 HandUpwardMovement => Vector3.up.ToAlvr() * (eyeHeight + handUpwardMovement);
        private Vector3 HandForwardMovement => Vector3.forward.ToAlvr() * handForwardMovement;

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

        private void Awake()
        {
            DeviceAdapter.GetTrackingDelegate += GetTracking;
            DeviceAdapter.OnRenderedDelegate += OnRendered;
        }

        private Tracking GetTracking(long frameIndex)
        {
            var lEyeFov = GetLEyeFov(alvrClient.EyeWidth, alvrClient.EyeHeight);
            var rEyeFov = GetREyeFov(lEyeFov);
            var headPose = GetHeadPose();
            _tracking.ipd = ipd;
            _tracking.battery = 100; // TODO use device value
            _tracking.plugged = 1; // TODO use device value
            _tracking.mounted = 1;
            _tracking.lEyeFov = lEyeFov;
            _tracking.rEyeFov = rEyeFov;
            _tracking.headPosePosition = new CVector3
            {
                x = headPose.position.x / spaceScale,
                y = (headPose.position.y + eyeHeight) / spaceScale,
                z = headPose.position.z / spaceScale
            };
            _tracking.headPoseOrientation = headPose.rotation.ToCStruct();
            if (handTracking != null)
            {
                handTracking.UpdateHandState();
                var lCtrlState = handTracking.LCtrlState;
                var rCtrlState = handTracking.RCtrlState;
                _tracking.lCtrl = new Controller
                {
                    enabled = (byte)(lCtrlState.Enabled ? 1 : 0),
                    buttons = lCtrlState.Buttons,
                    trackpadPositionX = lCtrlState.Input2DPosition.x,
                    trackpadPositionY = lCtrlState.Input2DPosition.y,
                    triggerValue = lCtrlState.Trigger,
                    gripValue = lCtrlState.Grip,
                    orientation = lCtrlState.Orientation.ToCStruct(),
                    position = AdjustHandPosition(lCtrlState.Position, headPose).ToCStruct()
                };
                _tracking.rCtrl = new Controller
                {
                    enabled = (byte)(rCtrlState.Enabled ? 1 : 0),
                    buttons = rCtrlState.Buttons,
                    trackpadPositionX = rCtrlState.Input2DPosition.x,
                    trackpadPositionY = rCtrlState.Input2DPosition.y,
                    triggerValue = rCtrlState.Trigger,
                    gripValue = rCtrlState.Grip,
                    orientation = rCtrlState.Orientation.ToCStruct(),
                    position = AdjustHandPosition(rCtrlState.Position, headPose).ToCStruct()
                };
            }

            _headPoseHistory.Add(frameIndex, headPose);
            return _tracking;
        }

        private Vector3 AdjustHandPosition(Vector3 handPosition, Pose headPose)
        {
            var relativeHandPosition = handPosition + HandUpwardMovement - headPose.position;
            var sightDirectionPosition =
                Quaternion.Inverse(headPose.rotation) * relativeHandPosition + HandForwardMovement;
            var scale = new Vector3(1f, 1f / spaceScale, 1f / spaceScale);
            return headPose.rotation * Vector3.Scale(sightDirectionPosition, scale) + headPose.position;
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

        public void ReadSettings(TrackingSettings settings)
        {
            settings.SpaceScale = spaceScale;
            settings.EyeHeight = eyeHeight;
            settings.Ipd = ipd;
            settings.DiagonalFovAngle = diagonalFovAngle;
            settings.FovRatioInner = fovRatioInner;
            settings.FovRatioOuter = fovRatioOuter;
            settings.FovRatioUpper = fovRatioUpper;
            settings.FovRatioLower = fovRatioLower;
            settings.ZoomRatio = zoomRatio;
            settings.HandUpwardMovement = handUpwardMovement;
            settings.HandForwardMovement = handForwardMovement;
        }

        public void ApplySettings(TrackingSettings settings)
        {
            spaceScale = settings.SpaceScale;
            eyeHeight = settings.EyeHeight;
            ipd = settings.Ipd;
            diagonalFovAngle = settings.DiagonalFovAngle;
            fovRatioInner = settings.FovRatioInner;
            fovRatioOuter = settings.FovRatioOuter;
            fovRatioUpper = settings.FovRatioUpper;
            fovRatioLower = settings.FovRatioLower;
            zoomRatio = settings.ZoomRatio;
            handUpwardMovement = settings.HandUpwardMovement;
            handForwardMovement = settings.HandForwardMovement;
        }
    }
}