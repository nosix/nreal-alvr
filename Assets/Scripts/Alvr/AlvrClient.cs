using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alvr
{
    public class AlvrClient : MonoBehaviour
    {
        [SerializeField] private string deviceName;
        [SerializeField] private int recommendedEyeWidth = 1920;
        [SerializeField] private int recommendedEyeHeight = 1080;
        [Range(0.1f, 1.0f)] [SerializeField] private float eyeSizeRatio = 1.0f;
        [SerializeField] private float[] availableRefreshRates = { 60f };
        [SerializeField] private float preferredRefreshRate = 60f;

        public int EyeWidth => (int) (recommendedEyeWidth * eyeSizeRatio);
        public int EyeHeight => (int) (recommendedEyeHeight * eyeSizeRatio);

        private AndroidJavaObject _androidPlugInInstance;

        private readonly DeviceSettings _deviceSettings = new DeviceSettings();

        [DllImport("alvr_android")]
        private static extern IntPtr GetInitContextEventFunc();

        private void Awake()
        {
            InitializeAndroidPlugin();
            InitContext();
            DeviceDataManager.DeviceSettingsProducer += GetDeviceSettings;
            _androidPlugInInstance?.Call("onAwake");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _androidPlugInInstance?.Call("onApplicationPause", pauseStatus);
        }

        private void OnDestroy()
        {
            DeviceDataManager.DeviceSettingsProducer -= GetDeviceSettings;
            _androidPlugInInstance?.Call("onDestroy");
            _androidPlugInInstance = null;
        }

        private void InitializeAndroidPlugin()
        {
            if (_androidPlugInInstance != null) return;
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _androidPlugInInstance = new AndroidJavaObject("io.github.alvr.android.lib.UnityPlugin", activity);
        }

        private DeviceSettings GetDeviceSettings()
        {
            _deviceSettings.name = deviceName;
            _deviceSettings.recommendedEyeWidth = EyeWidth;
            _deviceSettings.recommendedEyeHeight = EyeHeight;
            _deviceSettings.availableRefreshRates = availableRefreshRates;
            _deviceSettings.availableRefreshRatesLen = availableRefreshRates.Length;
            _deviceSettings.preferredRefreshRate = preferredRefreshRate;
            return _deviceSettings;
        }

        private static void InitContext()
        {
            GL.IssuePluginEvent(GetInitContextEventFunc(), 0);
        }

        public void AttachTexture(Texture texture)
        {
            _androidPlugInInstance?.Call(
                "attachTexture",
                (int)texture.GetNativeTexturePtr(),
                texture.width, texture.height
            );
        }
    }
}