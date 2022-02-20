using UnityEngine;
using UnityEngine.Android;

namespace Alvr
{
    public class Microphone : MonoBehaviour
    {
        [SerializeField] private PermissionRationalDialog permissionRationalDialog;

        private void Awake()
        {
            permissionRationalDialog.onSubmit.AddListener(OnRationalOk);
        }

        private void Start()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) return;
            Permission.RequestUserPermission(Permission.Microphone);
        }

        private void Update()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                if (permissionRationalDialog.gameObject.activeSelf)
                {
                    permissionRationalDialog.gameObject.SetActive(false);
                }
            }
            else
            {
                permissionRationalDialog.gameObject.SetActive(true);
            }
        }

        private static void OnRationalOk()
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
    }
}