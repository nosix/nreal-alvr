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
        [SerializeField] private float thresholdAngleEnableInput = 50f;
        [SerializeField] private float minDistance2DInput = 0.02f;
        [SerializeField] private float maxDistance2DInput = 0.1f;
        [SerializeField] private float thresholdDistanceBendThumb = 0.03f;
        [SerializeField] private float maxAngleForTrigger = 110f;
        [SerializeField] private float thresholdAngleForTrigger = 60f;
        [SerializeField] private float maxAngleForGrip = 110f;
        [SerializeField] private float thresholdAngleForGrip = 60f;
        [SerializeField] private float averageWindowMs = 500f;
        [SerializeField] private int averageWindowSamples = 20;

        [SerializeField] private GameObject buttonPanel;
        [SerializeField] private Image l2DInputIndicator;
        [SerializeField] private Image lGripIndicator;
        [SerializeField] private Image lTriggerIndicator;
        [SerializeField] private Image r2DInputIndicator;
        [SerializeField] private Image rGripIndicator;
        [SerializeField] private Image rTriggerIndicator;

        [SerializeField] private GameObject lHandModel;
        [SerializeField] private GameObject rHandModel;
        [SerializeField] private GameObject lHandPointer;
        [SerializeField] private GameObject rHandPointer;

        [SerializeField] private bool debug;

        private static readonly Quaternion RotateFrontFacing = Quaternion.AngleAxis(0f, Vector3.up);
        private static readonly Quaternion RotateBackFacing = Quaternion.AngleAxis(180f, Vector3.up);
        private static readonly int Value = Shader.PropertyToID("value");
        private static readonly int X = Shader.PropertyToID("x");
        private static readonly int Y = Shader.PropertyToID("y");

        private readonly Subject<Unit> _onUpdatedCtrlState = new Subject<Unit>();

        private struct Context
        {
            public IntervalTimeRecorder Interval;
            public bool InputEnabled;
            public bool ButtonEnabled;
            public MovingAverage PalmAngleWithFront;
            public MovingAverage PalmAngleWithBack;
            public Vector3? OriginOf2DInput;
            public HandControllerState CtrlState;

            public void Reset(int averageWindowSamples, float averageWindowMs)
            {
                Interval = new IntervalTimeRecorder(60);
                InputEnabled = false;
                ButtonEnabled = false;
                PalmAngleWithFront = new MovingAverage(
                    averageWindowSamples,
                    new DataSampleFilter(Interval, averageWindowMs, averageWindowSamples)
                );
                PalmAngleWithBack = new MovingAverage(
                    averageWindowSamples,
                    new DataSampleFilter(Interval, averageWindowMs, averageWindowSamples)
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

        private static float ToRatio(float value, float minAbsValue, float maxAbsValue)
        {
            var sign = value < 0 ? -1f : 1f;
            var absValue = Mathf.Abs(value);
            if (absValue < minAbsValue) return 0f;
            return sign * (absValue > maxAbsValue ? 1f : (absValue - minAbsValue) / (maxAbsValue - minAbsValue));
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

            buttonPanel.SetActive(false);
            l2DInputIndicator.enabled = false;
            r2DInputIndicator.enabled = false;
            lGripIndicator.enabled = false;
            rGripIndicator.enabled = false;
            lTriggerIndicator.enabled = false;
            rTriggerIndicator.enabled = false;

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
            _lContext.Reset(averageWindowSamples, averageWindowMs);
            _rContext.Reset(averageWindowSamples, averageWindowMs);
            _angleRangeForTrigger = maxAngleForTrigger - thresholdAngleForTrigger;
            _angleRangeForGrip = maxAngleForGrip - thresholdAngleForGrip;
            _activeButtonId = -1;
        }

        private void Start()
        {
            lHandModel.SetActive(debug);
            rHandModel.SetActive(debug);
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

            context.Interval.NextTick();

            var headPose = NRFrame.HeadPose;
            var inverseHeadRotation = Quaternion.Inverse(headPose.rotation.ToAlvr());

            var palm = state.GetJointPose(HandJointID.Palm);
            var palmRotation = inverseHeadRotation * palm.rotation.ToAlvr();
            var palmAngleWithFront = Quaternion.Angle(RotateFrontFacing, palmRotation);
            var palmAngleWithBack = Quaternion.Angle(RotateBackFacing, palmRotation);
            context.PalmAngleWithFront.Next(palmAngleWithFront);
            context.PalmAngleWithBack.Next(palmAngleWithBack);
            var palmIsFacingFront = context.PalmAngleWithFront.Average < thresholdAngleEnableInput;
            var palmIsFacingBack = context.PalmAngleWithBack.Average < thresholdAngleEnableInput;

            // Debug.Log($"Palm {palmIsFacingFront} {(int)context.PalmAngleWithFront.Average} {palmIsFacingBack} {(int)context.PalmAngleWithBack.Average}");

            context.CtrlState.Orientation = palm.rotation.ToAlvr();
            context.CtrlState.Position = palm.position;

            context.InputEnabled = palmIsFacingFront || palmIsFacingBack;
            context.ButtonEnabled = palmIsFacingBack;

            context.CtrlState.Buttons = 0;
            context.CtrlState.Trigger = 0f;
            context.CtrlState.Grip = 0f;
            context.CtrlState.Input2DPosition.x = 0f;
            context.CtrlState.Input2DPosition.y = 0f;

            // Ignore the input because it is easy to detect falsely
            if (!context.InputEnabled) return;

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);

            context.CtrlState.Buttons = MapButton(_activeButtonId);

            if (!context.ButtonEnabled)
            {
                // Trigger
                var indexAngle = Quaternion.Angle(palm.rotation, indexMiddle.rotation);
                var triggerAngle = indexAngle - thresholdAngleForTrigger;
                if (triggerAngle > 0f)
                {
                    context.CtrlState.Trigger =
                        triggerAngle > _angleRangeForTrigger ? 1f : triggerAngle / _angleRangeForTrigger;

                    if (context.CtrlState.Trigger > float.Epsilon)
                    {
                        context.CtrlState.Buttons |= ToFlag(AlvrInput.TriggerTouch);
                    }

                    if (1f - context.CtrlState.Trigger < float.Epsilon)
                    {
                        context.CtrlState.Buttons |= ToFlag(AlvrInput.TriggerClick);
                    }
                }

                // Grip
                var middleAngle = Quaternion.Angle(palm.rotation, middleMiddle.rotation);
                var gripAngle = middleAngle - thresholdAngleForGrip;
                if (gripAngle > 0f)
                {
                    context.CtrlState.Grip =
                        gripAngle > _angleRangeForGrip ? 1f : gripAngle / _angleRangeForGrip;

                    if (context.CtrlState.Grip > float.Epsilon)
                    {
                        context.CtrlState.Buttons |= ToFlag(AlvrInput.GripTouch);
                    }

                    if (1f - context.CtrlState.Grip < float.Epsilon)
                    {
                        context.CtrlState.Buttons |= ToFlag(AlvrInput.GripClick);
                    }
                }

                // Debug.Log($"Trigger/Grip {context.CtrlState.Trigger > 0f} {(int)context.CtrlState.Trigger} {(int)indexAngle} {context.CtrlState.Grip > 0f} {(int)context.CtrlState.Grip} {(int)middleAngle}");

                // Bend Thumb
                var thumbIndexDistance = Vector3.Distance(thumbDistal.position, indexProximal.position);
                var nearThumbIndexPosition = thumbIndexDistance < thresholdDistanceBendThumb;

                if (nearThumbIndexPosition)
                {
                    context.CtrlState.Buttons |= ToFlag(AlvrInput.JoystickTouch) | ToFlag(AlvrInput.TrackpadTouch);
                }

                // Debug.Log($"Bend Thumb {nearThumbIndexPosition} {(int)(thumbIndexDistance * 100)}");
            }

            // 2D Input (joystick, trackpad, etc.)
            if (context.ButtonEnabled)
            {
                if (context.OriginOf2DInput == null)
                {
                    context.OriginOf2DInput = palm.position;
                }
                else
                {
                    var moved = NRFrame.HeadPose.rotation * (palm.position - (Vector3)context.OriginOf2DInput);
                    context.CtrlState.Input2DPosition.x = ToRatio(moved.x, minDistance2DInput, maxDistance2DInput);
                    context.CtrlState.Input2DPosition.y = ToRatio(moved.y, minDistance2DInput, maxDistance2DInput);
                }
            }
            else
            {
                context.OriginOf2DInput = null;
            }

            // Debug.Log($"{context.CtrlState.Input2DPosition} {context.OriginOf2DInput}");
        }

        private void UpdateIndicators()
        {
            buttonPanel.SetActive(_lContext.ButtonEnabled || _rContext.ButtonEnabled);
            lHandPointer.SetActive(_rContext.ButtonEnabled);
            rHandPointer.SetActive(_lContext.ButtonEnabled);

            l2DInputIndicator.enabled = _lContext.ButtonEnabled;
            r2DInputIndicator.enabled = _rContext.ButtonEnabled;
            lGripIndicator.enabled = _lContext.InputEnabled;
            rGripIndicator.enabled = _rContext.InputEnabled;
            lTriggerIndicator.enabled = _lContext.InputEnabled;
            rTriggerIndicator.enabled = _rContext.InputEnabled;

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