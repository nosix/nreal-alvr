using UnityEngine;

namespace Application
{
    public class ObjectTracker : MonoBehaviour
    {
        [SerializeField] private Transform anchor;

        private void Update()
        {
            var thisTransform = transform;
            thisTransform.position = anchor.position;
            thisTransform.rotation = anchor.rotation;
        }
    }
}