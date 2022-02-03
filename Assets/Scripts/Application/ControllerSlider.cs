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
        private float _defaultValue;
        private float _cacheValue;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _controllerBridge = GetComponentInParent<ControllerBridge>();
            _defaultValue = _slider.value;
        }

        private void OnEnable()
        {
            OnChanged();
        }

        public void OnChanged()
        {
            _controllerBridge.OnChanged(sliderId, _slider.value);
        }

        public void Restore(bool cache)
        {
            if (cache)
            {
                _cacheValue = _slider.value;
                _slider.value = _defaultValue;
            }
            else
            {
                _slider.value = _cacheValue;
            }
        }
    }
}