using UnityEngine;

namespace Application
{
    public class AppConfig : MonoBehaviour
    {
        [SerializeField]
        private ScreenOrientation screenOrientation = ScreenOrientation.Portrait;

        private const string KeyTrackingSettings = "KeyTrackingSettings";

        private void Start()
        {
            Screen.orientation = screenOrientation;
        }

        public string TrackingSettings
        {
            get => PlayerPrefs.GetString(KeyTrackingSettings);
            set => PlayerPrefs.SetString(KeyTrackingSettings, value);
        }
    }
}