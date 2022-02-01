using UnityEngine;
using UnityEngine.UI;

namespace Application
{
    [RequireComponent(typeof(Slider))]
    public class ControllerSlider : MonoBehaviour
    {
        [SerializeField] private int sliderId;

        private Slider _slider;
        private ControllerBridge _controllerBridge;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _controllerBridge = GetComponentInParent<ControllerBridge>();
        }

        public void OnChanged()
        {
            _controllerBridge.OnChanged(sliderId, _slider.value);
        }
    }
}