using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Application
{
    public class ScreenMaterialControl : MonoBehaviour
    {
        [SerializeField] private Material screenMaterial;

        [SerializeField] private bool useFieldPose;
        [SerializeField] private Pose poseInRequest;
        [SerializeField] private Pose poseInRendered;

        private static readonly int PositionMoved = Shader.PropertyToID("PositionMoved");
        private static readonly int DirectionRotateAxis = Shader.PropertyToID("DirectionRotateAxis");
        private static readonly int DirectionAngle = Shader.PropertyToID("DirectionAngle");

        private void SetPose(Pose request, Pose rendered)
        {
            var positionMoved = rendered.position - request.position;
            var requestDirection = request.forward;
            var renderedDirection = rendered.forward;
            var directionAngle = Vector3.Angle(renderedDirection, requestDirection);
            var directionRotateAxis = directionAngle != 0f
                ? Vector3.Cross(renderedDirection, requestDirection)
                : renderedDirection;
            screenMaterial.SetVector(PositionMoved, positionMoved);
            screenMaterial.SetVector(DirectionRotateAxis, directionRotateAxis);
            screenMaterial.SetFloat(DirectionAngle, directionAngle);
        }

        private void Start()
        {
            if (useFieldPose) StartCoroutine(UpdatePose());
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator UpdatePose()
        {
            while (true)
            {
                SetPose(poseInRequest, poseInRendered);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}