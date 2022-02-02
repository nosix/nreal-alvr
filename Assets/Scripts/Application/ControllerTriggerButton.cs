using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Application
{
    [RequireComponent(typeof(Button))]
    public class ControllerTriggerButton : MonoBehaviour
    {
        [SerializeField] private GameObject iconEnabled;
        [SerializeField] private GameObject iconDisabled;
        [SerializeField] private UnityEvent<bool> onChanged;

        public void OnClick()
        {
            if (iconEnabled.activeSelf)
            {
                iconEnabled.SetActive(false);
                iconDisabled.SetActive(true);
                onChanged.Invoke(false);
            }
            else
            {
                iconEnabled.SetActive(true);
                iconDisabled.SetActive(false);
                onChanged.Invoke(true);
            }
        }
    }
}