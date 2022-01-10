using NRKernal;
using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    struct HandControllerState
    {
        public Vector2? input2DPosition;
        public float trigger;
        public float grip;
        public int button;
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
        [SerializeField] private float thresholdAngleForTwist = 40f;
        [SerializeField] private float twistAngleAverageWindowMs = 1000f;
        [SerializeField] private int averageWindowSamples = 120;

        [SerializeField] private Image l2DInput;
        [SerializeField] private Image lGrip;
        [SerializeField] private Image lTrigger;

        private static readonly UnityEngine.Quaternion RotateAroundY =
            UnityEngine.Quaternion.AngleAxis(90f, UnityEngine.Vector3.up);
        private static readonly int Value = Shader.PropertyToID("value");
        private static readonly int X = Shader.PropertyToID("x");
        private static readonly int Y = Shader.PropertyToID("y");

        private IntervalTimeRecorder _interval;
        private MovingAverage _palmAngleY;
        private float _angleRangeForTrigger;
        private float _angleRangeForGrip;

        private Material _l2DInputMaterial;
        private Material _lGripMaterial;
        private Material _lTriggerMaterial;

        private int _activeButtonId;
        private UnityEngine.Vector3? _lOriginOf2DInput;

        private static float AbsDeltaAngle(float angle1, float angle2)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angle1, angle2));
        }

        private static float ToRatio(float value, float maxAbsValue)
        {
            var sign = value < 0 ? -1f : 1f;
            var absValue = Mathf.Abs(value);
            return sign * (absValue > maxAbsValue ? 1f : absValue / maxAbsValue);
        }

        private void Awake()
        {
            _l2DInputMaterial = Instantiate(l2DInput.material);
            _lGripMaterial = Instantiate(lGrip.material);
            _lTriggerMaterial = Instantiate(lTrigger.material);

            l2DInput.material = _l2DInputMaterial;
            lGrip.material = _lGripMaterial;
            lTrigger.material = _lTriggerMaterial;
        }

        private void OnEnable()
        {
            _interval = new IntervalTimeRecorder(60);
            _palmAngleY = new MovingAverage(
                averageWindowSamples,
                new DataSampleFilter(_interval, twistAngleAverageWindowMs, averageWindowSamples)
            );
            _angleRangeForTrigger = maxAngleForTrigger - thresholdAngleForTrigger;
            _angleRangeForGrip = maxAngleForGrip - thresholdAngleForGrip;
            _activeButtonId = -1;
            _lOriginOf2DInput = null;
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

            // Ignore the input because it is easy to detect falsely while twisting
            if (deltaAngleY > thresholdAngleForTwist) return null;

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleProximal = state.GetJointPose(HandJointID.MiddleProximal);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);

            var controllerState = new HandControllerState
            {
                button = _activeButtonId
            };

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
                        ToRatio(moved.x, maxDistance2DInput),
                        ToRatio(moved.y, maxDistance2DInput)
                    );
                }
            }
            else
            {
                originOf2DInput = null;
            }

            Debug.Log($"{controllerState.button} {controllerState.trigger} {controllerState.grip} {controllerState.input2DPosition}");

            return controllerState;
        }

        public void PressButton(int buttonId)
        {
            _activeButtonId = buttonId;
        }

        public void ReleaseButton()
        {
            _activeButtonId = -1;
        }

        private void Update()
        {
            var lState = NRInput.Hands.GetHandState(HandEnum.LeftHand);
            var rState = NRInput.Hands.GetHandState(HandEnum.RightHand);
            var lCtrlState = ScanHandState(lState, ref _lOriginOf2DInput);
            if (lCtrlState != null) {
                _lGripMaterial.SetFloat(Value, lCtrlState.Value.grip);
                _lTriggerMaterial.SetFloat(Value, lCtrlState.Value.trigger);
                _l2DInputMaterial.SetFloat(X, lCtrlState.Value.input2DPosition?.x ?? 0f);
                _l2DInputMaterial.SetFloat(Y, lCtrlState.Value.input2DPosition?.y ?? 0f);
            }
        }
    }
}