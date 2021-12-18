using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class DeviceConnection : MonoBehaviour
    {
        [MenuItem("NRSDK/ConnectDevice", false, 1)]
        private static async void ConnectDevice()
        {
            var applicationRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
            Debug.Log(applicationRoot);
            var adbCommand = Path.Combine(applicationRoot!, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb");
            Debug.Log(adbCommand);
            var path = Path.GetDirectoryName(adbCommand);
            Debug.Log(path);
            var adbProcess = new Process
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
            adbProcess.Start();

            const int port = 5555;
            var command = string.Join(" && ",
                $@"PATH={path}:$PATH",
                @"adb disconnect",
                @"ipaddr=`adb shell ""ip -f inet -o addr show wlan0 | sed -e 's/^.*inet //' -e 's/\/.*$//'""`",
                $@"adb tcpip {port}",
                $@"adb connect $ipaddr:{port}",
                $@"scrcpy -s $ipaddr:{port} 2>&1"
            );

            var writer = adbProcess.StandardInput;
            await writer.WriteAsync(command);
            writer.Close();

            var stdOut = await adbProcess.StandardOutput.ReadToEndAsync();
            if (stdOut.Length != 0) Debug.Log(stdOut);

            var errOut = await adbProcess.StandardError.ReadToEndAsync();
            if (errOut.Length != 0) Debug.LogError(errOut);

            adbProcess.Close();
        }
    }
}