using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UniRx;

namespace Alvr
{
    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public class DeviceSettings
    {
        public string name;
        public int recommendedEyeWidth;
        public int recommendedEyeHeight;
        public float[] availableRefreshRates;
        public int availableRefreshRatesLen;
        public float preferredRefreshRate;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public class Tracking
    {
        /// Inter Pupillary Distance (meter)
        public float ipd;

        public byte battery;
        public byte plugged;
        public CRect lEyeFov = new CRect();
        public CRect rEyeFov = new CRect();
        public CQuaternion headPoseOrientation = new CQuaternion();
        public CVector3 headPosePosition = new CVector3();
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct CRect {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct CQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct CVector3
    {
        public float x;
        public float y;
        public float z;
    }

    public delegate DeviceSettings GetDeviceSettingsDelegate();
    public delegate Tracking GetTrackingDelegate(long frameIndex);
    public delegate void OnRenderedDelegate(long frameIndex);

    public static class DeviceAdapter
    {
        public static GetDeviceSettingsDelegate GetDeviceSettingsDelegate;
        public static GetTrackingDelegate GetTrackingDelegate;
        public static OnRenderedDelegate OnRenderedDelegate;

        private static readonly Subject<long> OnRenderedSubject = new Subject<long>();

        static DeviceAdapter()
        {
            OnRenderedSubject
                .ObserveOnMainThread()
                .Subscribe(frameIndex =>
                {
                    OnRenderedDelegate?.Invoke(frameIndex);
                });
            SetDeviceAdapter(
                GetDeviceSettings,
                GetTracking,
                OnRendered
            );
        }

        [DllImport("alvr_android")]
        private static extern void SetDeviceAdapter(
            GetDeviceSettingsDelegate getDeviceSettings,
            GetTrackingDelegate getTracking,
            OnRenderedDelegate onRendered
        );

        [AOT.MonoPInvokeCallbackAttribute(typeof(GetDeviceSettingsDelegate))]
        public static DeviceSettings GetDeviceSettings()
        {
            return GetDeviceSettingsDelegate?.Invoke();
        }

        [AOT.MonoPInvokeCallbackAttribute(typeof(GetTrackingDelegate))]
        public static Tracking GetTracking(long frameIndex)
        {
            return GetTrackingDelegate?.Invoke(frameIndex);
        }

        [AOT.MonoPInvokeCallbackAttribute(typeof(OnRenderedDelegate))]
        public static void OnRendered(long frameIndex)
        {
            OnRenderedSubject.OnNext(frameIndex);
        }
    }
}