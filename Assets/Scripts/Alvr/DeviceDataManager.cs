using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
        public int preferredRefreshRate;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public class Tracking
    {
        /// Inter Pupillary Distance (meter)
        public float ipd;

        public byte battery;
        public byte plugged;
        public Rect l_eye_fov = new Rect();
        public Rect r_eye_fov = new Rect();
        public Quaternion head_pose_orientation = new Quaternion();
        public Vector3 head_pose_position = new Vector3();
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")] // Accessed with native code
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    public delegate DeviceSettings DeviceSettingsProducerDelegate();

    public delegate Tracking TrackingProducerDelegate();

    public static class DeviceDataManager
    {
        public static DeviceSettingsProducerDelegate DeviceSettingsProducer;
        public static TrackingProducerDelegate TrackingProducer;

        static DeviceDataManager()
        {
            SetDeviceDataProducer(
                GetDeviceSettings,
                GetTracking
            );
        }

        [DllImport("alvr_android")]
        private static extern void SetDeviceDataProducer(
            DeviceSettingsProducerDelegate deviceSettingsProducer,
            TrackingProducerDelegate trackingProducer
        );

        [AOT.MonoPInvokeCallbackAttribute(typeof(DeviceSettingsProducerDelegate))]
        public static DeviceSettings GetDeviceSettings()
        {
            return DeviceSettingsProducer.Invoke();
        }

        [AOT.MonoPInvokeCallbackAttribute(typeof(TrackingProducerDelegate))]
        public static Tracking GetTracking()
        {
            return TrackingProducer.Invoke();
        }
    }
}