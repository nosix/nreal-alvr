using UnityEngine;
using UnityEngine.UI;

namespace Application
{
    [RequireComponent(typeof(Slider))]
    public class Switch : MonoBehaviour
    {
        [SerializeField] private int switchId;

        private Slider _slider;
        private ControllerBridge _controllerBridge;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _controllerBridge = GetComponentInParent<ControllerBridge>();
        }

        public void OnChanged()
        {
            _controllerBridge.OnChanged(switchId, _slider.value > 0.5f);
        }
    }
}