using UnityEngine;
using UnityEngine.Events;

namespace Alvr
{
    public class PermissionRationalDialog : MonoBehaviour
    {
        public UnityEvent onSubmit;

        public void OnClickOk()
        {
            onSubmit.Invoke();
        }
    }
}