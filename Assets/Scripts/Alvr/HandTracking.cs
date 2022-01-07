using NRKernal;
using UnityEngine;

namespace Alvr
{
    struct HandControllerState
    {
        public Vector2? input2DPosition;
        public float trigger;
        public float grip;
        public bool system;
        public bool button;
        public int buttonMode;
    }

    public class HandTracking : MonoBehaviour
    {
        [SerializeField] private Transform headAnchor;

        [SerializeField] private float maxDistance2DInput = 0.1f;
        [SerializeField] private float thresholdDistanceEnable2DInput = 0.03f;
        [SerializeField] private float thresholdAngleEnable2DInput = 25f;
        [SerializeField] private float maxAngleForTrigger = 90f;
        [SerializeField] private float thresholdAngleForTrigger = 60f;
        [SerializeField] private float maxAngleForGrip = 90f;
        [SerializeField] private float thresholdAngleForGrip = 50f;
        [SerializeField] private float thresholdAngleForButton = 20f;
        [SerializeField] private float thresholdAngleForSystem = 45f;
        [SerializeField] private float thresholdAngleForTwist = 40f;
        [SerializeField] private float thresholdAngleForModeChange = 90f;
        [SerializeField] private float palmAngleAverageWindowMs = 3000f;
        [SerializeField] private float twistAngleAverageWindowMs = 1000f;
        [SerializeField] private int averageWindowSamples = 120;
        [SerializeField] private int buttonModeNum = 3;

        private static readonly UnityEngine.Quaternion RotateAroundY =
            UnityEngine.Quaternion.AngleAxis(90f, UnityEngine.Vector3.up);

        private IntervalTimeRecorder _interval;
        private MovingAverage _palmAngleX;
        private MovingAverage _palmAngleY;
        private float _angleRangeForTrigger;
        private float _angleRangeForGrip;

        private int _buttonMode;
        private bool _changeButtonMode;

        private UnityEngine.Vector3? _lOriginOf2DInput;

        private static float AbsDeltaAngle(float angle1, float angle2)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angle1, angle2));
        }

        private void OnEnable()
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
            _angleRangeForTrigger = maxAngleForTrigger - thresholdAngleForTrigger;
            _angleRangeForGrip = maxAngleForGrip - thresholdAngleForGrip;
        }

        private HandControllerState? ScanHandState(HandState state, ref UnityEngine.Vector3? originOf2DInput)
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

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleProximal = state.GetJointPose(HandJointID.MiddleProximal);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);
            var ringProximal = state.GetJointPose(HandJointID.RingProximal);
            var ringMiddle = state.GetJointPose(HandJointID.RingMiddle);

            var controllerState = new HandControllerState();

            if (_changeButtonMode)
            {
                _changeButtonMode = false;
                _buttonMode = (_buttonMode + 1) % buttonModeNum;
            }

            controllerState.buttonMode = _buttonMode;

            // Button A/X, B/Y and 2D Input Press
            var palmAngleX = palm.rotation.eulerAngles.x; // about 0..90
            palmAngleX = (palmAngleX + 180f) % 360f; // 0 and 360 to be the same
            var palmAngleXAverage = _palmAngleX.Average;
            var deltaAngleX = Mathf.DeltaAngle(palmAngleXAverage, palmAngleX);

            if (-deltaAngleX > thresholdAngleForButton)
            {
                controllerState.button = true;
            }
            else
            {
                _palmAngleX.Next(palmAngleX);
            }

            // Debug.Log($"{controllerState.buttonMode} {controllerState.button} {(int)deltaAngleX} {(int)palmAngleX} {(int)palmAngleXAverage} {(int)palmAngleYAverage}");

            // Trigger
            var indexAngle = UnityEngine.Quaternion.Angle(indexProximal.rotation, indexMiddle.rotation);
            var triggerAngle = indexAngle - thresholdAngleForTrigger;
            if (triggerAngle > 0f)
            {
                controllerState.trigger =
                    triggerAngle > _angleRangeForTrigger ? 1f : triggerAngle / _angleRangeForTrigger;
            }

            // Grip
            var middleAngle = UnityEngine.Quaternion.Angle(middleProximal.rotation, middleMiddle.rotation);
            var gripAngle = middleAngle - thresholdAngleForGrip;
            if (gripAngle > 0f)
            {
                controllerState.grip = gripAngle > _angleRangeForGrip ? 1f : gripAngle / _angleRangeForGrip;
            }

            // Debug.Log($"Trigger/Grip {controllerState.trigger > 0f} {(int)controllerState.trigger} {(int)indexAngle} {controllerState.grip > 0f} {(int)controllerState.grip} {(int)middleAngle}");

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
                    var moved = headAnchor.rotation * (palm.position - (UnityEngine.Vector3)originOf2DInput);
                    controllerState.input2DPosition = new Vector2(
                        moved.x > maxDistance2DInput ? 1f : moved.x / maxDistance2DInput,
                        moved.y > maxDistance2DInput ? 1f : moved.y / maxDistance2DInput
                    );
                }
            }
            else
            {
                originOf2DInput = null;
            }

            // Special buttons such as Menu and System
            var ringAngle = UnityEngine.Quaternion.Angle(ringProximal.rotation, ringMiddle.rotation);
            var bendRing = ringAngle > thresholdAngleForSystem;
            if (state.currentGesture == HandGesture.Victory &&
                bendRing && controllerState.grip == 0f && controllerState.trigger == 0f)
            {
                controllerState.system = true;
            }

            Debug.Log($"{controllerState.system} {controllerState.buttonMode} {controllerState.button} {controllerState.trigger > 0f} {controllerState.grip > 0f} {controllerState.input2DPosition}");

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