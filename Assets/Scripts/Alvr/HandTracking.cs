using NRKernal;
using UnityEngine;

namespace Alvr
{
    struct HandControllerState
    {
        public Vector2? input2DPosition;
        public bool input2DPress;
        public bool trigger;
        public bool grip;
        public bool system;
        public bool button;
        public int buttonMode;
    }

    public class HandTracking : MonoBehaviour
    {
        [SerializeField] private float thresholdDistanceEnable2DInput = 0.03f;
        [SerializeField] private float thresholdAngleEnable2DInput = 25f;
        [SerializeField] private float thresholdAngleFor2DInputPress = 30f;
        [SerializeField] private float thresholdAngleForTrigger = 45f;
        [SerializeField] private float thresholdAngleForGrip = 45f;
        [SerializeField] private float thresholdAngleForButton = 20f;
        [SerializeField] private float thresholdAngleForSystem = 45f;
        [SerializeField] private float thresholdAngleForTwist = 40f;
        [SerializeField] private float thresholdAngleForModeChange = 90f;
        [SerializeField] private float palmAngleAverageWindowMs = 3000f;
        [SerializeField] private float twistAngleAverageWindowMs = 1000f;
        [SerializeField] private int averageWindowSamples = 120;
        [SerializeField] private int buttonModeNum = 2;

        private static readonly UnityEngine.Quaternion RotateAroundY =
            UnityEngine.Quaternion.AngleAxis(90f, UnityEngine.Vector3.up);

        private IntervalTimeRecorder _interval;
        private MovingAverage _palmAngleX;
        private MovingAverage _palmAngleY;

        private int _buttonMode;
        private bool _changeButtonMode;

        private Vector2? _lOriginOf2DInput;

        private static float AbsDeltaAngle(float angle1, float angle2)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angle1, angle2));
        }

        private void Awake()
        {
            _interval = new IntervalTimeRecorder(60);
            _palmAngleX = new MovingAverage(
                averageWindowSamples,
                new DataSampleFilter(_interval, palmAngleAverageWindowMs, averageWindowSamples)
            );
            _palmAngleY = new MovingAverage(
                averageWindowSamples,
                new DataSampleFilter(_interval, twistAngleAverageWindowMs, averageWindowSamples)
            );
        }

        private HandControllerState? ScanHandState(HandState state, ref Vector2? originOf2DInput)
        {
            _interval.NextTick();

            if (!state.isTracked) return null;

            var palm = state.GetJointPose(HandJointID.Palm);

            // Twist hand
            var palmAngleY = palm.rotation.eulerAngles.y;
            palmAngleY = (palmAngleY + 180f) % 360f; // 0 and 360 to be the same
            var palmAngleYAverage = _palmAngleY.Average;
            var deltaAngleY = AbsDeltaAngle(palmAngleYAverage, palmAngleY);
            _palmAngleY.Next(palmAngleY);

            // Debug.Log($"{(int)deltaAngleY} {_changeButtonMode}");

            if (deltaAngleY > thresholdAngleForModeChange)
            {
                _changeButtonMode = true;
            }

            // Ignore the input because it is easy to detect falsely while twisting
            if (deltaAngleY > thresholdAngleForTwist) return null;

            var thumbMetacarpal = state.GetJointPose(HandJointID.ThumbMetacarpal);
            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var thumbTip = state.GetJointPose(HandJointID.ThumbTip);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleProximal = state.GetJointPose(HandJointID.MiddleProximal);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);
            var ringProximal = state.GetJointPose(HandJointID.RingProximal);
            var ringMiddle = state.GetJointPose(HandJointID.RingMiddle);

            var controllerState = new HandControllerState();

            // Button A/X and B/Y
            var palmAngleX = palm.rotation.eulerAngles.x; // about 0..90
            palmAngleX = (palmAngleX + 180f) % 360f; // 0 and 360 to be the same
            var palmAngleXAverage = _palmAngleX.Average;
            var deltaAngleX = Mathf.DeltaAngle(palmAngleXAverage, palmAngleX);

            if (deltaAngleX > thresholdAngleForButton)
            {
                if (_changeButtonMode)
                {
                    _changeButtonMode = false;
                    _buttonMode = (_buttonMode + 1) % buttonModeNum;
                }

                controllerState.button = true;
            }
            else
            {
                _palmAngleX.Next(palmAngleX);
            }

            controllerState.buttonMode = _buttonMode;

            // Debug.Log($"{controllerState.buttonMode} {controllerState.button} {(int)deltaAngleX} {(int)palmAngleX} {(int)palmAngleXAverage} {(int)palmAngleYAverage}");

            // Trigger
            var indexAngle = UnityEngine.Quaternion.Angle(indexProximal.rotation, indexMiddle.rotation);
            if (indexAngle > thresholdAngleForTrigger)
            {
                controllerState.trigger = true;
            }

            // Grip
            var middleAngle = UnityEngine.Quaternion.Angle(middleProximal.rotation, middleMiddle.rotation);
            if (middleAngle > thresholdAngleForGrip)
            {
                controllerState.grip = true;
            }

            // Debug.Log($"Trigger/Grip {controllerState.trigger} {(int)indexAngle} {controllerState.grip} {(int)middleAngle}");

            // 2D Input Press
            var thumbAngle = UnityEngine.Quaternion.Angle(thumbMetacarpal.rotation, thumbTip.rotation);
            if (thumbAngle > thresholdAngleFor2DInputPress)
            {
                controllerState.input2DPress = true;
            }

            // Debug.Log($"2D Input Press {controllerState.input2DPress} {(int)thumbAngle}");

            // 2D Input (joystick, trackpad, etc.)
            var thumbIndexDistance = UnityEngine.Vector3.Distance(thumbDistal.position, indexProximal.position);
            var nearThumbIndexPosition = thumbIndexDistance < thresholdDistanceEnable2DInput;

            var thumbIndexAngle =
                UnityEngine.Quaternion.Angle(thumbDistal.rotation * RotateAroundY, indexProximal.rotation);
            var nearThumbIndexAngle = thumbIndexAngle < thresholdAngleEnable2DInput;

            var enable2DInput = nearThumbIndexPosition || nearThumbIndexAngle;

            // Debug.Log($"2D Input {enable2DInput} {thumbIndexDistance} {(int)thumbIndexAngle}");

            if (enable2DInput)
            {
                if (originOf2DInput == null)
                {
                    originOf2DInput = palm.position;
                }
                else
                {
                    controllerState.input2DPosition = palm.position - originOf2DInput;
                }
            }
            else
            {
                originOf2DInput = null;
            }

            // Special buttons such as Menu and System
            var ringAngle = UnityEngine.Quaternion.Angle(ringProximal.rotation, ringMiddle.rotation);
            var bendRing = ringAngle > thresholdAngleForSystem;
            if (!bendRing && state.currentGesture == HandGesture.Victory)
            {
                controllerState.system = true;
            }

            return controllerState;
        }

        private void Update()
        {
            var lState = NRInput.Hands.GetHandState(HandEnum.LeftHand);
            var rState = NRInput.Hands.GetHandState(HandEnum.RightHand);
            ScanHandState(lState, ref _lOriginOf2DInput);
        }
    }
}