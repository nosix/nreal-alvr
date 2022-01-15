using UnityEngine;

namespace Application
{
    public class ControllerBridge : MonoBehaviour
    {
        public void OnPressed(int buttonId)
        {
            Debug.Log($"{buttonId}");
        }

        public void OnChanged(int switchId, bool isOn)
        {
            Debug.Log($"{switchId} {isOn}");
        }
    }
}