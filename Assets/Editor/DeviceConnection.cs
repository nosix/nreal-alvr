using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class DeviceConnection : MonoBehaviour
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

        private static async Task<string> GetDeviceIpAddress(bool refresh = false)
        {
            if (!refresh && _deviceIpAddress != null) return _deviceIpAddress;

            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                @"adb disconnect 1> /dev/null",　// To have only one device with usb connection
                @"adb shell ""ip -f inet -o addr show wlan0 | sed -e 's/^.*inet //' -e 's/\/.*$//'"""
            );

            using var bashProcess = StartBashProcess();

            var writer = bashProcess.StandardInput;
            await writer.WriteAsync(command);
            writer.Close();

            var errOut = await bashProcess.StandardError.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);

            var ipAddress = await bashProcess.StandardOutput.ReadToEndAsync();

            _deviceIpAddress = ipAddress.Trim();

            return _deviceIpAddress;
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
            var ipAddress = await GetDeviceIpAddress(true);
            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"adb tcpip {Port}",
                $@"adb connect {ipAddress}:{Port}",
                $@"scrcpy -s {ipAddress}:{Port} 2>&1"
            );
            Run(command);
        }

        [MenuItem("NRSDK/Scrcpy", false, 1)]
        private static async void Scrcpy()
        {
            if (_deviceIpAddress == null)
            {
                Debug.LogError("Please execute ConnectDeviceAsRemote first");
                return;
            }

            var ipAddress = await GetDeviceIpAddress();
            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"scrcpy -s {ipAddress}:{Port} 2>&1"
            );
            Run(command);
        }

        [MenuItem("NRSDK/ShowDevices", false, 1)]
        private static void ShowDevices()
        {
            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                @"adb devices"
            );
            Run(command);
        }

        [MenuItem("NRSDK/RebootRemoteDevice", false, 1)]
        private static async void RebootRemoteDevice()
        {
            var ipAddress = await GetDeviceIpAddress();
            var command = string.Join(" && ",
                $@"PATH={GetAdbPath()}:$PATH",
                $@"adb -s {ipAddress}:{Port} shell reboot"
            );
            Run(command);

            _deviceIpAddress = null;
        }
    }
}