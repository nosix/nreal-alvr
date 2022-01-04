using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Application
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ScreenMaterialControl : MonoBehaviour
    {
        [SerializeField] private bool enablePoseProperty = true;

        [SerializeField] private bool useFieldPose;

        // Since RemoteInspector does not support Pose type, position and rotation are separated.
        [SerializeField] private Vector3 positionInRequest;
        [SerializeField] private Vector3 rotationInRequest;
        [SerializeField] private Vector3 positionInRendered;
        [SerializeField] private Vector3 rotationInRendered;

        private static readonly int PositionMoved = Shader.PropertyToID("PositionMoved");
        private static readonly int DirectionRotateAxis = Shader.PropertyToID("DirectionRotateAxis");
        private static readonly int DirectionAngle = Shader.PropertyToID("DirectionAngle");

        private Material _material;

        private void Awake()
        {
            _material = GetComponent<MeshRenderer>().sharedMaterial;
        }

        public void SetPose(Pose request, Pose rendered)
        {
            SetPoseInternal(request, rendered, enablePoseProperty);
        }

        private void SetPoseInternal(Pose request, Pose rendered, bool usePose)
        {
            if (!usePose || _material == null) return;
            var positionMoved = rendered.position - request.position;
            var rotation = rendered.rotation * Quaternion.Inverse(request.rotation);
            rotation.ToAngleAxis(out var directionAngle, out var directionRotateAxis);
            _material.SetVector(PositionMoved, positionMoved);
            _material.SetVector(DirectionRotateAxis, directionRotateAxis);
            _material.SetFloat(DirectionAngle, directionAngle);
            Debug.Log($"{positionMoved} {directionRotateAxis} {directionAngle}");
        }

        private void OnEnable()
        {
            if (useFieldPose)
            {
                StartCoroutine(nameof(UpdatePose));
            }
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator UpdatePose()
        {
            while (true)
            {
                var poseInRequest = new Pose(positionInRequest, Quaternion.Euler(rotationInRequest));
                var poseInRendered = new Pose(positionInRendered, Quaternion.Euler(rotationInRendered));
                SetPoseInternal(poseInRequest, poseInRendered, enablePoseProperty);
                yield return new WaitForEndOfFrame();
            }
        }

        private void OnDisable()
        {
            StopCoroutine(nameof(UpdatePose));
            // Reset shared material
            SetPose(Pose.identity, Pose.identity);
        }
    }
}