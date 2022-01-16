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

        public void OnReleased()
        {
            handTracking.ReleaseButton();
        }

        public void OnChanged(int switchId, bool isOn)
        {
            switch (switchId)
            {
                case 1:
                    handTracking.SetButtonPanelEnabled(isOn);
                    break;
            }
        }
    }
}