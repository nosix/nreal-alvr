using NRKernal;
using UnityEngine;
using UnityEngine.Events;

namespace Alvr
{
    public class TrackingNreal : MonoBehaviour
    {
        [SerializeField] private AlvrClient alvrClient;
        [SerializeField] private float eyeHeight = 1.55f;
        [SerializeField] private HandTracking handTracking;
        [SerializeField] private UnityEvent<Pose, Pose> onRendered;

        private const float DiagonalFovAngle = 52f;

        private readonly Tracking _tracking = new Tracking();
        private readonly HeadPoseHistory _headPoseHistory = new HeadPoseHistory();

        private static readonly Vector3 RotateDirection = new Vector3(-1f, -1f, 1f);

        private static CRect GetEyeFov(float diagonalFovAngle, float width, float height)
        {
            var screenDiagonalAngleFromAdjacent = Mathf.Atan(height / width);
            var screenWidthAngle = Mathf.Cos(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            var screenHeightAngle = Mathf.Sin(screenDiagonalAngleFromAdjacent) * diagonalFovAngle;
            return new CRect
            {
                left = screenWidthAngle,
                right = screenWidthAngle,
                top = screenHeightAngle,
                bottom = screenHeightAngle
            };
        }

        private void Awake()
        {
            DeviceAdapter.GetTrackingDelegate += GetTracking;
            DeviceAdapter.OnRenderedDelegate += OnRendered;
        }

        private Tracking GetTracking(long frameIndex)
        {
            var eyeFov = GetEyeFov(DiagonalFovAngle, alvrClient.EyeWidth, alvrClient.EyeHeight);
            var headPose = GetHeadPose();
            _tracking.ipd = 0.068606f;
            _tracking.battery = 100;
            _tracking.plugged = 1;
            _tracking.lEyeFov = eyeFov;
            _tracking.rEyeFov = eyeFov;
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
                _tracking.lCtrl = new Controller
                {
                    buttons = lCtrlState.Buttons,
                    trackpadPositionX = lCtrlState.Input2DPosition.x,
                    trackpadPositionY = lCtrlState.Input2DPosition.y,
                    triggerValue = lCtrlState.Trigger,
                    gripValue = lCtrlState.Grip,
                    orientation = new CQuaternion
                    {
                        x = lCtrlState.Orientation.x,
                        y = lCtrlState.Orientation.y,
                        z = lCtrlState.Orientation.z,
                        w = lCtrlState.Orientation.w
                    },
                    position = new CVector3
                    {
                        x = lCtrlState.Position.x,
                        y = lCtrlState.Position.y,
                        z = lCtrlState.Position.z
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
                        x = rCtrlState.Orientation.x,
                        y = rCtrlState.Orientation.y,
                        z = rCtrlState.Orientation.z,
                        w = rCtrlState.Orientation.w
                    },
                    position = new CVector3
                    {
                        x = rCtrlState.Position.x,
                        y = rCtrlState.Position.y,
                        z = rCtrlState.Position.z
                    }
                };
            }
            _headPoseHistory.Add(frameIndex, headPose);
            return _tracking;
        }

        private static Pose GetHeadPose()
        {
            var headPosePosition = NRFrame.HeadPose.position;
            var headPoseRotation = Quaternion.Euler(
                Vector3.Scale(NRFrame.HeadPose.rotation.eulerAngles, RotateDirection)
            );
            return new Pose(headPosePosition, headPoseRotation);
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