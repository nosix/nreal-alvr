using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alvr
{
    public class AlvrClient : MonoBehaviour
    {
        [SerializeField] private string deviceName;
        [SerializeField] private int recommendedEyeWidth;
        [SerializeField] private int recommendedEyeHeight;
        [SerializeField] private float[] availableRefreshRates;
        [SerializeField] private int preferredRefreshRate;

        private AndroidJavaObject _androidPlugInInstance;

        [DllImport("alvr_android")]
        private static extern IntPtr GetInitContextEventFunc();

        [DllImport("alvr_android")]
        private static extern void SetDeviceSettings(DeviceSettings settings);

        private void Awake()
        {
            InitializeAndroidPlugin();
            InitContext();
            DeviceDataManager.Producer += OnDataRequested;
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
            DeviceDataManager.Producer -= OnDataRequested;
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

        private void OnDataRequested(byte dataKind)
        {
            switch (dataKind)
            {
                case 1:
                    SetDeviceSettings(new DeviceSettings
                    {
                        name = deviceName,
                        recommendedEyeWidth = recommendedEyeWidth,
                        recommendedEyeHeight = recommendedEyeHeight,
                        availableRefreshRates = availableRefreshRates,
                        availableRefreshRatesLen = availableRefreshRates.Length,
                        preferredRefreshRate = preferredRefreshRate
                    });
                    break;
            }
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