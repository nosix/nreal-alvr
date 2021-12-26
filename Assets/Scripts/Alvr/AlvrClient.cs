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
        [SerializeField] private float[] availableRefreshRates = {60f};
        [SerializeField] private float preferredRefreshRate = 60f;

        private AndroidJavaObject _androidPlugInInstance;

        [DllImport("alvr_android")]
        private static extern IntPtr GetInitContextEventFunc();

        private void Awake()
        {
            InitializeAndroidPlugin();
            InitContext();
            DeviceDataManager.DeviceSettingsProducer += GetDeviceSettings;
            _androidPlugInInstance?.Call("onAwake");
        }

        private void OnEnable()
        {
            _androidPlugInInstance?.Call("onEnable");
        }

        private void OnDisable()
        {
            _androidPlugInInstance?.Call("onDisable");
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
            // TODO Set the DeviceSettings lifetime to static
            return new DeviceSettings
            {
                name = deviceName,
                recommendedEyeWidth = (int)(recommendedEyeWidth * eyeSizeRatio),
                recommendedEyeHeight = (int)(recommendedEyeHeight * eyeSizeRatio),
                availableRefreshRates = availableRefreshRates,
                availableRefreshRatesLen = availableRefreshRates.Length,
                preferredRefreshRate = preferredRefreshRate
            };
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