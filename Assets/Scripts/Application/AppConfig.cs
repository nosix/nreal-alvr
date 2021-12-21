using UnityEngine;

namespace Application
{
    public class AppConfig : MonoBehaviour
    {
        [SerializeField]
        private ScreenOrientation screenOrientation = ScreenOrientation.Landscape;

        private void Start()
        {
            Screen.orientation = screenOrientation;
        }
    }
}