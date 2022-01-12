using System.Collections;
using System.Diagnostics.CodeAnalysis;
using NRKernal;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    public struct HandControllerState
    {
        public Quaternion Orientation;
        public Vector3 Position;
        public Vector2 Input2DPosition;
        public float Trigger;
        public float Grip;
        public ulong Buttons;
    }

    public class HandTracking : MonoBehaviour
    {
        [SerializeField] private float thresholdAngleEnableInput = 90f;
        [SerializeField] private float maxDistance2DInput = 0.1f;
        [SerializeField] private float thresholdDistanceEnable2DInput = 0.03f;
        [SerializeField] private float thresholdAngleEnable2DInput = 25f;
        [SerializeField] private float maxAngleForTrigger = 90f;
        [SerializeField] private float thresholdAngleForTrigger = 60f;
        [SerializeField] private float maxAngleForGrip = 90f;
        [SerializeField] private float thresholdAngleForGrip = 50f;
        [SerializeField] private float thresholdAngleForTwist = 40f;
        [SerializeField] private float twistAngleAverageWindowMs = 500f;
        [SerializeField] private int averageWindowSamples = 20;

        [SerializeField] private GameObject buttonPanel;
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
        private const float NegativeAngleRange = 90f;

        private readonly Subject<Unit> _onUpdatedCtrlState = new Subject<Unit>();

        private struct Context
        {
            public IntervalTimeRecorder Interval;
            public bool InputEnabled;
            public MovingAverage PalmAngleY;
            public Vector3? OriginOf2DInput;
            public HandControllerState CtrlState;

            public void Reset(int averageWindowSamples, float twistAngleAverageWindowMs)
            {
                Interval = new IntervalTimeRecorder(60);
                InputEnabled = false;
                PalmAngleY = new MovingAverage(
                    averageWindowSamples,
                    new DataSampleFilter(Interval, twistAngleAverageWindowMs, averageWindowSamples)
                );
                OriginOf2DInput = null;
                CtrlState = new HandControllerState();
            }
        }

        private float _angleRangeForTrigger;
        private float _angleRangeForGrip;

        private Material _l2DInputMaterial;
        private Material _lGripMaterial;
        private Material _lTriggerMaterial;
        private Material _r2DInputMaterial;
        private Material _rGripMaterial;
        private Material _rTriggerMaterial;

        private int _activeButtonId;

        private Context _lContext;
        private Context _rContext;

        public HandControllerState LCtrlState => _lContext.CtrlState;
        public HandControllerState RCtrlState => _rContext.CtrlState;

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

            _onUpdatedCtrlState
                .ObserveOnMainThread()
                .Subscribe(_ => UpdateIndicators());
        }

        private void OnDestroy()
        {
            StopCoroutine(nameof(UpdateHandStateLoop));
            Destroy(_l2DInputMaterial);
            Destroy(_lGripMaterial);
            Destroy(_lTriggerMaterial);
            Destroy(_r2DInputMaterial);
            Destroy(_rGripMaterial);
            Destroy(_rTriggerMaterial);
        }

        private void OnEnable()
        {
            _lContext.Reset(averageWindowSamples, twistAngleAverageWindowMs);
            _rContext.Reset(averageWindowSamples, twistAngleAverageWindowMs);
            _angleRangeForTrigger = maxAngleForTrigger - thresholdAngleForTrigger;
            _angleRangeForGrip = maxAngleForGrip - thresholdAngleForGrip;
            _activeButtonId = -1;
        }

        private void Start()
        {
            if (debug) StartCoroutine(nameof(UpdateHandStateLoop));
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator UpdateHandStateLoop()
        {
            while (true)
            {
                UpdateHandStateInternal();
                yield return new WaitForEndOfFrame();
            }
        }

        public void UpdateHandState()
        {
            if (!debug) UpdateHandStateInternal();
        }

        private void UpdateHandStateInternal()
        {
            var lState = NRInput.Hands.GetHandState(HandEnum.LeftHand);
            var rState = NRInput.Hands.GetHandState(HandEnum.RightHand);
            ScanHandState(lState, ref _lContext);
            ScanHandState(rState, ref _rContext);
            _onUpdatedCtrlState.OnNext(Unit.Default);
        }

        private void ScanHandState(HandState state, ref Context context)
        {
            if (!state.isTracked) return;

            var palm = state.GetJointPose(HandJointID.Palm);

            context.CtrlState.Orientation = palm.rotation;
            context.CtrlState.Position = palm.position;

            context.Interval.NextTick();

            // Twist hand
            var palmAngleY = palm.rotation.eulerAngles.y;
            palmAngleY = (palmAngleY + NegativeAngleRange) % 360f; // 0 and 360 to be the same
            var palmAngleYAverage = context.PalmAngleY.Average;
            var deltaAngleY = AbsDeltaAngle(palmAngleYAverage, palmAngleY);
            context.PalmAngleY.Next(palmAngleY);

            // Ignore the input because it is easy to detect falsely while twisting
            if (deltaAngleY > thresholdAngleForTwist) return;

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleProximal = state.GetJointPose(HandJointID.MiddleProximal);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);

            context.CtrlState.Buttons = MapButton(_activeButtonId);

            context.InputEnabled = context.PalmAngleY.Average > thresholdAngleEnableInput + NegativeAngleRange;

            if (!context.InputEnabled)
            {
                // Trigger
                var indexAngle = Quaternion.Angle(indexProximal.rotation, indexMiddle.rotation);
                var triggerAngle = indexAngle - thresholdAngleForTrigger;
                if (triggerAngle > 0f)
                {
                    context.CtrlState.Trigger =
                        triggerAngle > _angleRangeForTrigger ? 1f : triggerAngle / _angleRangeForTrigger;
                }
                else
                {
                    context.CtrlState.Trigger = 0f;
                }

                // Grip
                var middleAngle = Quaternion.Angle(middleProximal.rotation, middleMiddle.rotation);
                var gripAngle = middleAngle - thresholdAngleForGrip;
                if (gripAngle > 0f)
                {
                    context.CtrlState.Grip = gripAngle > _angleRangeForGrip ? 1f : gripAngle / _angleRangeForGrip;
                }
                else
                {
                    context.CtrlState.Grip = 0f;
                }

                // Debug.Log($"Trigger/Grip {context.CtrlState.Trigger > 0f} {(int)context.CtrlState.Trigger} {(int)indexAngle} {context.CtrlState.Grip > 0f} {(int)context.CtrlState.Grip} {(int)middleAngle}");
            }

            return;

            // 2D Input (joystick, trackpad, etc.)
            // FIXME There are many recognition mistakes
            var thumbIndexDistance = Vector3.Distance(thumbDistal.position, indexProximal.position);
            var nearThumbIndexPosition = thumbIndexDistance < thresholdDistanceEnable2DInput;

            var thumbIndexAngle =
                Quaternion.Angle(thumbDistal.rotation * RotateAroundY, indexProximal.rotation);
            var nearThumbIndexAngle = thumbIndexAngle < thresholdAngleEnable2DInput;

            var enable2DInput = nearThumbIndexPosition || nearThumbIndexAngle;

            // Debug.Log($"2D Input {enable2DInput} {thumbIndexDistance} {(int)thumbIndexAngle}");

            context.CtrlState.Input2DPosition.x = 0f;
            context.CtrlState.Input2DPosition.y = 0f;

            if (enable2DInput)
            {
                if (context.OriginOf2DInput == null)
                {
                    context.OriginOf2DInput = palm.position;
                }
                else
                {
                    var moved = NRFrame.HeadPose.rotation * (palm.position - (Vector3)context.OriginOf2DInput);
                    context.CtrlState.Input2DPosition.x = ToRatio(moved.x, maxDistance2DInput);
                    context.CtrlState.Input2DPosition.y = ToRatio(moved.y, maxDistance2DInput);
                }
            }
            else
            {
                context.OriginOf2DInput = null;
            }

            // Debug.Log($"{controllerState.button} {controllerState.trigger} {controllerState.grip} {controllerState.input2DPosition}");
        }

        private void UpdateIndicators()
        {
            switch (_lContext.InputEnabled || _rContext.InputEnabled)
            {
                case true when !buttonPanel.activeSelf:
                    buttonPanel.SetActive(true);
                    break;
                case false when buttonPanel.activeSelf:
                    buttonPanel.SetActive(false);
                    break;
            }

            _lGripMaterial.SetFloat(Value, _lContext.CtrlState.Grip);
            _lTriggerMaterial.SetFloat(Value, _lContext.CtrlState.Trigger);
            _l2DInputMaterial.SetFloat(X, _lContext.CtrlState.Input2DPosition.x);
            _l2DInputMaterial.SetFloat(Y, _lContext.CtrlState.Input2DPosition.y);
            _rGripMaterial.SetFloat(Value, _rContext.CtrlState.Grip);
            _rTriggerMaterial.SetFloat(Value, _rContext.CtrlState.Trigger);
            _r2DInputMaterial.SetFloat(X, _rContext.CtrlState.Input2DPosition.x);
            _r2DInputMaterial.SetFloat(Y, _rContext.CtrlState.Input2DPosition.y);
        }

        public void PressButton(int buttonId)
        {
            _activeButtonId = buttonId;
        }

        public void ReleaseButton()
        {
            _activeButtonId = -1;
        }

        private static ulong MapButton(int buttonId)
        {
            return buttonId switch
            {
                0 => ToFlag(AlvrInput.JoystickTouch)
                     | ToFlag(AlvrInput.JoystickClick)
                     | ToFlag(AlvrInput.JoystickLeftClick)
                     | ToFlag(AlvrInput.TrackpadTouch)
                     | ToFlag(AlvrInput.TrackpadClick),
                1 => ToFlag(AlvrInput.ATouch) | ToFlag(AlvrInput.AClick),
                2 => ToFlag(AlvrInput.BTouch) | ToFlag(AlvrInput.BClick),
                3 => ToFlag(AlvrInput.ApplicationMenuClick),
                4 => ToFlag(AlvrInput.SystemClick),
                5 => ToFlag(AlvrInput.XTouch) | ToFlag(AlvrInput.XClick),
                6 => ToFlag(AlvrInput.YTouch) | ToFlag(AlvrInput.YClick),
                7 => ToFlag(AlvrInput.JoystickTouch)
                     | ToFlag(AlvrInput.JoystickClick)
                     | ToFlag(AlvrInput.JoystickRightClick)
                     | ToFlag(AlvrInput.TrackpadTouch)
                     | ToFlag(AlvrInput.TrackpadClick),
                _ => 0
            };
        }

        private static ulong ToFlag(AlvrInput input)
        {
            return 1UL << (int)input;
        }
    }
}