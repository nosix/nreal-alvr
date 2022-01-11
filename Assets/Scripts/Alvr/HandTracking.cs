using System.Collections;
using System.Diagnostics.CodeAnalysis;
using NRKernal;
using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    public struct HandControllerState
    {
        public Vector2 input2DPosition;
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

        [SerializeField] private Image l2DInputIndicator;
        [SerializeField] private Image lGripIndicator;
        [SerializeField] private Image lTriggerIndicator;
        [SerializeField] private Image r2DInputIndicator;
        [SerializeField] private Image rGripIndicator;
        [SerializeField] private Image rTriggerIndicator;

        [SerializeField] private bool debug;

        private static readonly Quaternion RotateAroundY = Quaternion.AngleAxis(90f, Vector3.up);
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
        private Material _r2DInputMaterial;
        private Material _rGripMaterial;
        private Material _rTriggerMaterial;

        private int _activeButtonId;
        private Vector3? _lOriginOf2DInput;
        private Vector3? _rOriginOf2DInput;
        private HandControllerState _lCtrlState;
        private HandControllerState _rCtrlState;

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
            _l2DInputMaterial = Instantiate(l2DInputIndicator.material);
            _lGripMaterial = Instantiate(lGripIndicator.material);
            _lTriggerMaterial = Instantiate(lTriggerIndicator.material);
            _r2DInputMaterial = Instantiate(r2DInputIndicator.material);
            _rGripMaterial = Instantiate(rGripIndicator.material);
            _rTriggerMaterial = Instantiate(rTriggerIndicator.material);

            l2DInputIndicator.material = _l2DInputMaterial;
            lGripIndicator.material = _lGripMaterial;
            lTriggerIndicator.material = _lTriggerMaterial;
            r2DInputIndicator.material = _r2DInputMaterial;
            rGripIndicator.material = _rGripMaterial;
            rTriggerIndicator.material = _rTriggerMaterial;
        }

        private void OnDestroy()
        {
            StopCoroutine(nameof(ScanHandStateLoop));
            Destroy(_l2DInputMaterial);
            Destroy(_lGripMaterial);
            Destroy(_lTriggerMaterial);
            Destroy(_r2DInputMaterial);
            Destroy(_rGripMaterial);
            Destroy(_rTriggerMaterial);
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
            _rOriginOf2DInput = null;
        }

        private void Start()
        {
            if (debug) StartCoroutine(nameof(ScanHandStateLoop));
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator ScanHandStateLoop()
        {
            while (true)
            {
                ScanHandState(ref _lCtrlState, ref _rCtrlState);
                yield return new WaitForEndOfFrame();
            }
        }

        public void ScanHandState(ref HandControllerState lCtrlState, ref HandControllerState rCtrlState)
        {
            _interval.NextTick();
            var lState = NRInput.Hands.GetHandState(HandEnum.LeftHand);
            var rState = NRInput.Hands.GetHandState(HandEnum.RightHand);
            ScanHandState(lState, ref lCtrlState, ref _lOriginOf2DInput);
            ScanHandState(rState, ref rCtrlState, ref _rOriginOf2DInput);
            _lGripMaterial.SetFloat(Value, lCtrlState.grip);
            _lTriggerMaterial.SetFloat(Value, lCtrlState.trigger);
            _l2DInputMaterial.SetFloat(X, lCtrlState.input2DPosition.x);
            _l2DInputMaterial.SetFloat(Y, lCtrlState.input2DPosition.y);
            _rGripMaterial.SetFloat(Value, rCtrlState.grip);
            _rTriggerMaterial.SetFloat(Value, rCtrlState.trigger);
            _r2DInputMaterial.SetFloat(X, rCtrlState.input2DPosition.x);
            _r2DInputMaterial.SetFloat(Y, rCtrlState.input2DPosition.y);
        }

        private void ScanHandState(
            HandState state,
            ref HandControllerState ctrlState,
            ref Vector3? originOf2DInput
        )
        {
            if (!state.isTracked) return;

            var palm = state.GetJointPose(HandJointID.Palm);

            // Twist hand
            var palmAngleY = palm.rotation.eulerAngles.y;
            palmAngleY = (palmAngleY + 180f) % 360f; // 0 and 360 to be the same
            var palmAngleYAverage = _palmAngleY.Average;
            var deltaAngleY = AbsDeltaAngle(palmAngleYAverage, palmAngleY);
            _palmAngleY.Next(palmAngleY);

            // Ignore the input because it is easy to detect falsely while twisting
            if (deltaAngleY > thresholdAngleForTwist) return;

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleProximal = state.GetJointPose(HandJointID.MiddleProximal);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);

            ctrlState.button = _activeButtonId;

            // Trigger
            var indexAngle = Quaternion.Angle(indexProximal.rotation, indexMiddle.rotation);
            var triggerAngle = indexAngle - thresholdAngleForTrigger;
            if (triggerAngle > 0f)
            {
                ctrlState.trigger =
                    triggerAngle > _angleRangeForTrigger ? 1f : triggerAngle / _angleRangeForTrigger;
            }
            else
            {
                ctrlState.trigger = 0f;
            }

            // Grip
            var middleAngle = Quaternion.Angle(middleProximal.rotation, middleMiddle.rotation);
            var gripAngle = middleAngle - thresholdAngleForGrip;
            if (gripAngle > 0f)
            {
                ctrlState.grip = gripAngle > _angleRangeForGrip ? 1f : gripAngle / _angleRangeForGrip;
            }
            else
            {
                ctrlState.grip = 0f;
            }

            // Debug.Log($"Trigger/Grip {controllerState.trigger > 0f} {(int)controllerState.trigger} {(int)indexAngle} {controllerState.grip > 0f} {(int)controllerState.grip} {(int)middleAngle}");

            // 2D Input (joystick, trackpad, etc.)
            var thumbIndexDistance = Vector3.Distance(thumbDistal.position, indexProximal.position);
            var nearThumbIndexPosition = thumbIndexDistance < thresholdDistanceEnable2DInput;

            var thumbIndexAngle =
                Quaternion.Angle(thumbDistal.rotation * RotateAroundY, indexProximal.rotation);
            var nearThumbIndexAngle = thumbIndexAngle < thresholdAngleEnable2DInput;

            var enable2DInput = nearThumbIndexPosition || nearThumbIndexAngle;

            // Debug.Log($"2D Input {enable2DInput} {thumbIndexDistance} {(int)thumbIndexAngle}");

            ctrlState.input2DPosition.x = 0f;
            ctrlState.input2DPosition.y = 0f;

            if (enable2DInput)
            {
                if (originOf2DInput == null)
                {
                    originOf2DInput = palm.position;
                }
                else
                {
                    var moved = headAnchor.rotation * (palm.position - (Vector3)originOf2DInput);
                    ctrlState.input2DPosition.x = ToRatio(moved.x, maxDistance2DInput);
                    ctrlState.input2DPosition.y = ToRatio(moved.y, maxDistance2DInput);
                }
            }
            else
            {
                originOf2DInput = null;
            }

            // Debug.Log($"{controllerState.button} {controllerState.trigger} {controllerState.grip} {controllerState.input2DPosition}");
        }

        public void PressButton(int buttonId)
        {
            _activeButtonId = buttonId;
        }

        public void ReleaseButton()
        {
            _activeButtonId = -1;
        }
    }
}