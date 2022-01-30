using System.Collections;
using System.Diagnostics.CodeAnalysis;
using NRKernal;
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
        [SerializeField] private SafeAngle anglePalmFacingFront = new SafeAngle
        {
            min = new Vector3(340f, 300f, 260f),
            max = new Vector3(370f, 370f, 400f)
        };

        [SerializeField] private float thresholdAnglePalmFacingBack = 60f;
        [SerializeField] private float thresholdDistanceEnableTracking = 0.3f;
        [SerializeField] private float minDistance2DInput = 0.02f;
        [SerializeField] private float maxDistance2DInput = 0.1f;
        [SerializeField] private float thresholdDistanceBendThumb = 0.03f;
        [SerializeField] private float maxAngleForTrigger = 120f;
        [SerializeField] private float thresholdAngleForTrigger = 80f;
        [SerializeField] private float maxAngleForGrip = 120f;
        [SerializeField] private float thresholdAngleForGrip = 80f;

        [SerializeField] private float sigmaWForAngle = 1f;
        [SerializeField] private float sigmaVForAngle = 10f;
        [SerializeField] private float sigmaWForAxis = 0.1f;
        [SerializeField] private float sigmaVForAxis = 1f;
        [SerializeField] private float sigmaWForPosition = 1e-5f;
        [SerializeField] private float sigmaVForPosition = 1e-4f;

        [SerializeField] private GameObject buttonPanel;
        [SerializeField] private Image l2DInputIndicator;
        [SerializeField] private Image lGripIndicator;
        [SerializeField] private Image lTriggerIndicator;
        [SerializeField] private Image lInputEnabledIndicator;
        [SerializeField] private Image lThumbPressedIndicator;
        [SerializeField] private Image r2DInputIndicator;
        [SerializeField] private Image rGripIndicator;
        [SerializeField] private Image rTriggerIndicator;
        [SerializeField] private Image rInputEnabledIndicator;
        [SerializeField] private Image rThumbPressedIndicator;

        [SerializeField] private GameObject lHandModel;
        [SerializeField] private GameObject rHandModel;
        [SerializeField] private GameObject lHandPointer;
        [SerializeField] private GameObject rHandPointer;

        [SerializeField] private bool debug;

        private static readonly Quaternion RotateBackFacing = Quaternion.AngleAxis(180f, Vector3.up);
        private static readonly int Value = Shader.PropertyToID("value");
        private static readonly int X = Shader.PropertyToID("x");
        private static readonly int Y = Shader.PropertyToID("y");

        private static readonly ulong
            FlagThumbTouch = ToFlag(AlvrInput.JoystickTouch) | ToFlag(AlvrInput.TrackpadTouch);

        private struct Context
        {
            public bool InputEnabled;
            public bool Input2DEnabled;
            public bool ButtonPanelEnabled;
            public SafeAngle AnglePalmFacingFront;
            public LocalLevelModelKalmanFilter PalmAngleWithBack;
            public LocalLevelModelKalmanFilter PalmRotationAngle;
            public LocalLevelModelKalmanFilter PalmRotationAxisX;
            public LocalLevelModelKalmanFilter PalmRotationAxisY;
            public LocalLevelModelKalmanFilter PalmRotationAxisZ;
            public LocalLevelModelKalmanFilter PalmPositionX;
            public LocalLevelModelKalmanFilter PalmPositionY;
            public LocalLevelModelKalmanFilter PalmPositionZ;
            public LocalLevelModelKalmanFilter IndexAngle;
            public LocalLevelModelKalmanFilter MiddleAngle;
            public LocalLevelModelKalmanFilter ThumbIndexDistance;
            public Vector3? OriginOf2DInput;
            public HandControllerState CtrlState;

            public void Reset(
                SafeAngle anglePalmFacingFront,
                float sigmaWForAngle, float sigmaVForAngle,
                float sigmaWForAxis, float sigmaVForAxis,
                float sigmaWForPosition, float sigmaVForPosition
            )
            {
                anglePalmFacingFront.Reset();
                InputEnabled = false;
                Input2DEnabled = false;
                ButtonPanelEnabled = false;
                AnglePalmFacingFront = anglePalmFacingFront;
                PalmAngleWithBack = new LocalLevelModelKalmanFilter(sigmaWForAngle, sigmaVForAngle);
                PalmRotationAngle = new LocalLevelModelKalmanFilter(sigmaWForAngle, sigmaVForAngle);
                PalmRotationAxisX = new LocalLevelModelKalmanFilter(sigmaWForAxis, sigmaVForAxis);
                PalmRotationAxisY = new LocalLevelModelKalmanFilter(sigmaWForAxis, sigmaVForAxis);
                PalmRotationAxisZ = new LocalLevelModelKalmanFilter(sigmaWForAxis, sigmaVForAxis);
                PalmPositionX = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
                PalmPositionY = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
                PalmPositionZ = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
                IndexAngle = new LocalLevelModelKalmanFilter(sigmaWForAngle, sigmaVForAngle);
                MiddleAngle = new LocalLevelModelKalmanFilter(sigmaWForAngle, sigmaVForAngle);
                ThumbIndexDistance = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
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

        private bool _buttonPanelEnabled;
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
            _lContext.Reset(
                anglePalmFacingFront,
                sigmaWForAngle, sigmaVForAngle,
                sigmaWForAxis, sigmaVForAxis,
                sigmaWForPosition, sigmaVForPosition
            );
            _rContext.Reset(
                anglePalmFacingFront.Mirror(),
                sigmaWForAngle, sigmaVForAngle,
                sigmaWForAxis, sigmaVForAxis,
                sigmaWForPosition, sigmaVForPosition
            );
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
        }

        private void ScanHandState(HandState state, ref Context context)
        {
            context.CtrlState.Buttons = 0;
            context.CtrlState.Trigger = 0f;
            context.CtrlState.Grip = 0f;
            context.CtrlState.Input2DPosition.x = 0f;
            context.CtrlState.Input2DPosition.y = 0f;

            if (!state.isTracked) return;

            var headPose = NRFrame.HeadPose;
            var inverseHeadRotation = Quaternion.Inverse(headPose.rotation.ToAlvr());

            var palm = state.GetJointPose(HandJointID.Palm);
            var yDistance = Mathf.Abs(headPose.position.y -  palm.position.y);

            var palmIsFacingBack = false;
            var palmIsFacingFront = false;

            if (yDistance < thresholdDistanceEnableTracking)
            {
                var palmRotation = inverseHeadRotation * palm.rotation.ToAlvr();
                var palmAngleWithBack = Quaternion.Angle(RotateBackFacing, palmRotation);
                context.PalmAngleWithBack.Next(palmAngleWithBack);
                palmIsFacingBack = context.PalmAngleWithBack.Value < thresholdAnglePalmFacingBack;
                palmIsFacingFront = context.AnglePalmFacingFront.Contains(palmRotation.eulerAngles);
            }

            // Debug.Log($"Palm {(int)(yDistance * 100)} {palmIsFacingFront} {palmIsFacingBack} {(int)context.PalmAngleWithBack.Value}");

            context.CtrlState.Orientation = palm.rotation.ToAlvr();
            context.CtrlState.Position = palm.position.ToAlvr();

            context.CtrlState.Orientation.ToAngleAxis(out var angle, out var axis);
            context.PalmRotationAngle.Update(ref angle);
            context.PalmRotationAxisX.Update(ref axis.x);
            context.PalmRotationAxisY.Update(ref axis.y);
            context.PalmRotationAxisZ.Update(ref axis.z);
            context.CtrlState.Orientation = Quaternion.AngleAxis(angle, axis);

            context.PalmPositionX.Update(ref context.CtrlState.Position.x);
            context.PalmPositionY.Update(ref context.CtrlState.Position.y);
            context.PalmPositionZ.Update(ref context.CtrlState.Position.z);

            context.InputEnabled = palmIsFacingFront || palmIsFacingBack;
            context.Input2DEnabled = palmIsFacingBack;
            context.ButtonPanelEnabled = _buttonPanelEnabled && palmIsFacingBack;

            // Ignore the input because it is easy to detect falsely
            if (!context.InputEnabled) return;

            var thumbDistal = state.GetJointPose(HandJointID.ThumbDistal);
            var indexProximal = state.GetJointPose(HandJointID.IndexProximal);
            var indexMiddle = state.GetJointPose(HandJointID.IndexMiddle);
            var middleMiddle = state.GetJointPose(HandJointID.MiddleMiddle);

            context.CtrlState.Buttons = MapButton(_activeButtonId);

            if (!context.ButtonPanelEnabled)
            {
                var palmInverseRotation = Quaternion.Inverse(palm.rotation);

                // Trigger
                var indexRotation = palmInverseRotation * indexMiddle.rotation;
                var indexAngle = (360f - indexRotation.eulerAngles.y + 90f) % 360f; // Range from 90 to 270
                context.IndexAngle.Next(indexAngle);

                var triggerAngle = context.IndexAngle.Value - 90f - thresholdAngleForTrigger;
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
                var middleRotation = palmInverseRotation * middleMiddle.rotation;
                var middleAngle = (360f - middleRotation.eulerAngles.y + 90f) % 360f; // Range from 90 to 270
                context.MiddleAngle.Next(middleAngle);

                var gripAngle = context.MiddleAngle.Value - 90f - thresholdAngleForGrip;
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

                // Debug.Log($"Trigger/Grip {context.CtrlState.Trigger > 0f} {(int)(context.CtrlState.Trigger * 100)} {(int)indexAngle} {context.CtrlState.Grip > 0f} {(int)(context.CtrlState.Grip * 100)} {(int)middleAngle}");

                // Bend Thumb
                var thumbIndexDistance = Vector3.Distance(thumbDistal.position, indexProximal.position);
                context.ThumbIndexDistance.Next(thumbIndexDistance);
                var nearThumbIndexPosition = context.ThumbIndexDistance.Value < thresholdDistanceBendThumb;

                if (nearThumbIndexPosition)
                {
                    context.CtrlState.Buttons |= FlagThumbTouch;
                }

                // Debug.Log($"Bend Thumb {nearThumbIndexPosition} {(int)(context.ThumbIndexDistance.Value * 100)}");
            }

            // 2D Input (joystick, trackpad, etc.)
            if (context.Input2DEnabled)
            {
                if (context.OriginOf2DInput == null)
                {
                    context.OriginOf2DInput = context.CtrlState.Position;
                }
                else
                {
                    var moved = NRFrame.HeadPose.rotation *
                                (context.CtrlState.Position - (Vector3)context.OriginOf2DInput);
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

        private void Update()
        {
            UpdateIndicators();
        }

        private void UpdateIndicators()
        {
            buttonPanel.SetActive(_lContext.ButtonPanelEnabled || _rContext.ButtonPanelEnabled);
            lHandPointer.SetActive(_rContext.ButtonPanelEnabled);
            rHandPointer.SetActive(_lContext.ButtonPanelEnabled);

            l2DInputIndicator.enabled = _lContext.Input2DEnabled;
            r2DInputIndicator.enabled = _rContext.Input2DEnabled;
            lGripIndicator.enabled = _lContext.InputEnabled;
            rGripIndicator.enabled = _rContext.InputEnabled;
            lTriggerIndicator.enabled = _lContext.InputEnabled;
            rTriggerIndicator.enabled = _rContext.InputEnabled;
            lInputEnabledIndicator.enabled = _lContext.InputEnabled;
            rInputEnabledIndicator.enabled = _rContext.InputEnabled;
            lThumbPressedIndicator.enabled = (_lContext.CtrlState.Buttons & FlagThumbTouch) != 0;
            rThumbPressedIndicator.enabled = (_rContext.CtrlState.Buttons & FlagThumbTouch) != 0;

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

        public void SetButtonPanelEnabled(bool isEnabled)
        {
            _buttonPanelEnabled = isEnabled;
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
                1 => ToFlag(AlvrInput.XTouch) | ToFlag(AlvrInput.XClick),
                2 => ToFlag(AlvrInput.YTouch) | ToFlag(AlvrInput.YClick),
                3 => ToFlag(AlvrInput.ApplicationMenuClick),
                4 => ToFlag(AlvrInput.SystemClick),
                5 => ToFlag(AlvrInput.ATouch) | ToFlag(AlvrInput.AClick),
                6 => ToFlag(AlvrInput.BTouch) | ToFlag(AlvrInput.BClick),
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