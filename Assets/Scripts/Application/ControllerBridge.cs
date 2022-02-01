using Alvr;
using UnityEngine;

namespace Application
{
    public class ControllerBridge : MonoBehaviour
    {
        [SerializeField] private HandTracking handTracking;

        public void OnPressed(int buttonId)
        {
            handTracking.PressButton(buttonId);
        }

        public void OnReleased(int buttonId)
        {
            handTracking.ReleaseButton(buttonId);
        }

        public void OnChanged(int sliderId, float value)
        {
            switch (sliderId)
            {
                case 1:
                    handTracking.SetButtonPanelEnabled(value > 0.5f);
                    break;
                case 2:
                    handTracking.SetAdditionalPalmRotationX(value);
                    break;
                case 3:
                    handTracking.SetAdditionalPalmRotationY(value);
                    break;
                case 4:
                    handTracking.SetAdditionalPalmRotationZ(value);
                    break;
            }
        }
    }
}