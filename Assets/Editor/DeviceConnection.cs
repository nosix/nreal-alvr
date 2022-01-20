using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class DeviceConnection : EditorWindow
    {
        private const int Port = 5555;
        [CanBeNull] private static string _deviceIpAddress;

        private static async Task<string> GetDeviceIpAddress()
        {
            using var command = new BashCommand(string.Join(" && ",
                Adb.SetPathEnvVar,
                @"adb reconnect offline 1> /dev/null",
                @"adb disconnect 1> /dev/null", // To have only one device with usb connection
                @"adb shell ""ip -f inet -o addr show wlan0 | sed -e 's/^.*inet //' -e 's/\/.*$//'"""
            ));

            await command.StartProcess();

            var errOut = await command.StdErr.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);

            var ipAddress = await command.StdOut.ReadToEndAsync();

            return ipAddress.Trim();
        }

        private static string GetOption()
        {
            return _deviceIpAddress == null ? "" : $@" -s {_deviceIpAddress}:{Port}";
        }

        private static async void Run(string command)
        {
            using var c = new BashCommand(command);
            await c.StartProcess();

            var stdOut = await c.StdOut.ReadToEndAsync();
            if (stdOut.Length != 0) Debug.Log(stdOut);

            var errOut = await c.StdErr.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);
        }

        [MenuItem("NRSDK/ConnectDeviceAsRemote", false, 1)]
        private static async void ConnectDeviceAsRemote()
        {
            _deviceIpAddress = await GetDeviceIpAddress();
            Debug.Log($"Cache IP address: {_deviceIpAddress}");

            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb tcpip {Port}",
                $@"adb connect {_deviceIpAddress}:{Port}",
                $@"scrcpy -s {_deviceIpAddress}:{Port} 2>&1"
            ));
        }

        [MenuItem("NRSDK/Scrcpy", false, 1)]
        private static void Scrcpy()
        {
            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"scrcpy{GetOption()} 2>&1"
            ));
        }

        [MenuItem("NRSDK/ShowDevices", false, 1)]
        private static void ShowDevices()
        {
            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                @"adb reconnect offline",
                @"adb devices"
            ));
        }

        [MenuItem("NRSDK/RebootRemoteDevice", false, 1)]
        private static void RebootRemoteDevice()
        {
            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb{GetOption()} shell reboot"
            ));

            _deviceIpAddress = null;
        }

        [MenuItem("NRSDK/ShutdownRemoteDevice", false, 1)]
        private static void ShutdownRemoteDevice()
        {
            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb{GetOption()} shell reboot -p"
            ));

            _deviceIpAddress = null;
        }

        [MenuItem("NRSDK/StartAppWithDeepProfile", false, 1)]
        private static void StartAppWithDeepProfile()
        {
            if (_deviceIpAddress != null)
            {
                Debug.LogWarning("This command must be run over a USB connection.");
            }

            var bundleIdentifier = UnityEngine.Application.identifier;
            Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                "adb disconnect",
                $@"adb shell am start -n {bundleIdentifier}/com.unity3d.player.UnityPlayerActivity -e 'unity' '-deepprofiling'",
                "scrcpy 2>&1"
            ));

            _deviceIpAddress = null;
        }
    }
}