using UnityEngine;

namespace Application
{
    public class ControllerButton : MonoBehaviour
    {
        [SerializeField] private int buttonId;

        private ControllerBridge _controllerBridge;

        private void Awake()
        {
            _controllerBridge = GetComponentInParent<ControllerBridge>();
        }

        public void OnPressed()
        {
            _controllerBridge.OnPressed(buttonId);
        }
    }
}