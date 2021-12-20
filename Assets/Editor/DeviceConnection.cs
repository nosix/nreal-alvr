using System.Diagnostics;
using System.IO;
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

        private static string GetAdbPath()
        {
            var applicationRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
            var adbCommand = Path.Combine(applicationRoot!, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb");
            return Path.GetDirectoryName(adbCommand);
        }

        private static Process StartBashProcess()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/bash",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();

            return process;
        }

        private static async Task<string> GetDeviceIpAddress()
        {
            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                @"adb disconnect 1> /dev/null", // To have only one device with usb connection
                @"adb shell ""ip -f inet -o addr show wlan0 | sed -e 's/^.*inet //' -e 's/\/.*$//'"""
            );

            using var bashProcess = StartBashProcess();

            var writer = bashProcess.StandardInput;
            await writer.WriteAsync(command);
            writer.Close();

            var errOut = await bashProcess.StandardError.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);

            var ipAddress = await bashProcess.StandardOutput.ReadToEndAsync();

            return ipAddress.Trim();
        }

        private static async void Run(string command)
        {
            using var bashProcess = StartBashProcess();

            var writer = bashProcess.StandardInput;
            await writer.WriteAsync(command);
            writer.Close();

            var stdOut = await bashProcess.StandardOutput.ReadToEndAsync();
            if (stdOut.Length != 0) Debug.Log(stdOut);

            var errOut = await bashProcess.StandardError.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);
        }

        [MenuItem("NRSDK/ConnectDeviceAsRemote", false, 1)]
        private static async void ConnectDeviceAsRemote()
        {
            _deviceIpAddress = await GetDeviceIpAddress();

            Run(string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"adb tcpip {Port}",
                $@"adb connect {_deviceIpAddress}:{Port}",
                $@"scrcpy -s {_deviceIpAddress}:{Port} 2>&1"
            ));
        }

        [MenuItem("NRSDK/Scrcpy", false, 1)]
        private static void Scrcpy()
        {
            if (_deviceIpAddress == null)
            {
                Debug.LogError("Please execute ConnectDeviceAsRemote first");
                return;
            }

            Run(string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"scrcpy -s {_deviceIpAddress}:{Port} 2>&1"
            ));
        }

        [MenuItem("NRSDK/ShowDevices", false, 1)]
        private static void ShowDevices()
        {
            Run(string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                @"adb devices"
            ));
        }

        [MenuItem("NRSDK/RebootRemoteDevice", false, 1)]
        private static void RebootRemoteDevice()
        {
            if (_deviceIpAddress == null) return;

            Run(string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"adb -s {_deviceIpAddress}:{Port} shell reboot"
            ));

            _deviceIpAddress = null;
        }

        [MenuItem("NRSDK/ShutdownRemoteDevice", false, 1)]
        private static void ShutdownRemoteDevice()
        {
            if (_deviceIpAddress == null) return;

            Run(string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"adb -s {_deviceIpAddress}:{Port} shell reboot -p"
            ));

            _deviceIpAddress = null;
        }
    }
}