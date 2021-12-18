using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Alvr
{
    public delegate void DeviceDataProducer(byte dataKind);

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

    public static class DeviceDataManager
    {
        public static DeviceDataProducer Producer;

        static DeviceDataManager()
        {
            SetDeviceDataProducer(OnDataRequested);
        }

        [DllImport("alvr_android")]
        private static extern void SetDeviceDataProducer(DeviceDataProducer producer);

        [AOT.MonoPInvokeCallbackAttribute(typeof(DeviceDataProducer))]
        public static void OnDataRequested(byte dataKind)
        {
            Producer.Invoke(dataKind);
        }
    }
}