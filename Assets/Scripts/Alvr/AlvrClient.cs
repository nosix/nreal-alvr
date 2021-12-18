using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alvr
{
    internal delegate void DeviceDataProducer(byte dataKind);

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    internal class DeviceSettings
    {
        public string name;
        public int recommendedEyeWidth;
        public int recommendedEyeHeight;
        public float[] availableRefreshRates;
        public int availableRefreshRatesLen;
        public int preferredRefreshRate;
    }

    public class AlvrClient : MonoBehaviour
    {
        private AndroidJavaObject _androidPlugInInstance;

        [DllImport("alvr_android")]
        private static extern IntPtr GetInitContextEventFunc();

        [DllImport("alvr_android")]
        private static extern void SetDeviceDataProducer(DeviceDataProducer producer);

        [DllImport("alvr_android")]
        private static extern void SetDeviceSettings(DeviceSettings settings);

        private void Awake()
        {
            InitializeAndroidPlugin();
            InitContext();
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
            _androidPlugInInstance?.Call("onDestroy");
            _androidPlugInInstance = null;
        }

        private void InitializeAndroidPlugin()
        {
            if (_androidPlugInInstance != null) return;
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _androidPlugInInstance = new AndroidJavaObject("io.github.alvr.android.lib.UnityPlugin", activity);
            SetDeviceDataProducer(OnDataRequested);
        }

        [AOT.MonoPInvokeCallbackAttribute(typeof(DeviceDataProducer))]
        private static void OnDataRequested(byte dataKind)
        {
            switch (dataKind)
            {
                case 1:
                    var refreshRates = new float[] { 60 };
                    SetDeviceSettings(new DeviceSettings
                    {
                        name = "Unity ALVR",
                        recommendedEyeWidth = 1920,
                        recommendedEyeHeight = 1080,
                        availableRefreshRates = refreshRates,
                        availableRefreshRatesLen = refreshRates.Length,
                        preferredRefreshRate = 60
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