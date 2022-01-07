using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    [RequireComponent(typeof(Image))]
    public class HandButton : MonoBehaviour
    {
        [SerializeField] private int buttonId;
        [SerializeField] private HandTracking handTracking;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color activeColor;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.color = defaultColor;
        }

        public void PressButton()
        {
            _image.color = activeColor;
            handTracking.PressButton(buttonId);
        }

        public void ReleaseButton()
        {
            _image.color = defaultColor;
            handTracking.ReleaseButton(buttonId);
        }
    }
}