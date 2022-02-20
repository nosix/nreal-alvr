using System;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class DeviceConnection : EditorWindow
    {
        private const int Port = 5555;

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

        private static async Task<string> GetConnectedDevice()
        {
            var lines = await Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                @"adb reconnect offline",
                @"adb devices"
            ));

            var deviceName = "";
            foreach (var line in lines.Split('\n'))
            {
                var serialNamePair = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (serialNamePair.Length != 2 || serialNamePair[1] != "device") continue;

                if (serialNamePair[0].EndsWith(Port.ToString()))
                {
                    deviceName = serialNamePair[0];
                    break;
                }

                if (deviceName.Length == 0) deviceName = serialNamePair[0];
            }

            return deviceName;
        }

        private static async Task<string> GetOption()
        {
            var device = await GetConnectedDevice();
            return device.Length == 0 ? "" : $@" -s {device}";
        }

        private static async Task<string> Run(string command)
        {
            using var c = new BashCommand(command);
            await c.StartProcess();

            var errOut = await c.StdErr.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);

            return await c.StdOut.ReadToEndAsync();
        }

        [MenuItem("NRSDK/ConnectDeviceAsRemote", false, 1)]
        private static async void ConnectDeviceAsRemote()
        {
            var deviceIpAddress = await GetDeviceIpAddress();
            var result = Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb tcpip {Port}",
                $@"adb connect {deviceIpAddress}:{Port}",
                $@"scrcpy -s {deviceIpAddress}:{Port} 2>&1"
            ));
            Debug.Log(await result);
        }

        [MenuItem("NRSDK/Scrcpy", false, 1)]
        private static async void Scrcpy()
        {
            var result = Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"scrcpy{await GetOption()} 2>&1"
            ));
            Debug.Log(await result);
        }

        [MenuItem("NRSDK/ShowDevices", false, 1)]
        private static async void ShowDevices()
        {
            var result = Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                @"adb reconnect offline",
                @"adb devices"
            ));
            Debug.Log(await result);
        }

        [MenuItem("NRSDK/RebootDevice", false, 1)]
        private static async void RebootDevice()
        {
            Debug.Log("Reboot device");
            var result = await Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb{await GetOption()} shell reboot"
            ));
            Debug.Log(result.Length != 0 ? result : "Please wait because it is restarting...");
        }

        [MenuItem("NRSDK/ShutdownDevice", false, 1)]
        private static async void ShutdownDevice()
        {
            Debug.Log("Shutdown device");
            var result = await Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                $@"adb{await GetOption()} shell reboot -p"
            ));
            if (result.Length != 0) Debug.Log(result);
        }

        [MenuItem("NRSDK/StartAppWithDeepProfile", false, 1)]
        private static async void StartAppWithDeepProfile()
        {
            Debug.LogWarning("This command must be run over a USB connection.");

            var bundleIdentifier = UnityEngine.Application.identifier;
            var result = Run(string.Join(" && ",
                Adb.SetPathEnvVar,
                "adb disconnect",
                $@"adb shell am start -n {bundleIdentifier}/com.unity3d.player.UnityPlayerActivity -e 'unity' '-deepprofiling'",
                "scrcpy 2>&1"
            ));
            Debug.Log(await result);
        }

        [MenuItem("NRSDK/GetAdbPath", false, 1)]
        private static void GetAdbPath()
        {
            Debug.Log(Adb.GetPath());
        }
    }
}