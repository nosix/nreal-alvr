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

    public delegate DeviceSettings DeviceSettingsProducerDelegate();

    public static class DeviceDataManager
    {
        public static DeviceSettingsProducerDelegate DeviceSettingsProducer;

        static DeviceDataManager()
        {
            SetDeviceDataProducer(
                GetDeviceSettings
            );
        }

        [DllImport("alvr_android")]
        private static extern void SetDeviceDataProducer(
            DeviceSettingsProducerDelegate deviceSettingsProducer
        );

        [AOT.MonoPInvokeCallbackAttribute(typeof(DeviceSettingsProducerDelegate))]
        public static DeviceSettings GetDeviceSettings()
        {
            return DeviceSettingsProducer.Invoke();
        }
    }
}