using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TimeSeriesDataViewer : EditorWindow
    {
        [MenuItem("NRSDK/TimeSeriesDataViewer")]
        private static void ShowWindow()
        {
            var window = GetWindow<TimeSeriesDataViewer>();
            window.titleContent = new GUIContent("Time Series Data Viewer");
            window.Show();
        }

        private string _deviceId;
        private bool _isRunning;
        private Task _logcatJob;

        private void OnEnable()
        {
            var devicesCommand = new BashCommand(string.Join(" && ",
                Adb.SetPathEnvVar,
                "adb devices"
            ));

            devicesCommand.StartProcess();

            var errMessage = devicesCommand.StdErr.ReadToEnd();
            if (errMessage.Length != 0)
            {
                Debug.LogError(errMessage);
                _deviceId = "Not connected to the device.";
                return;
            }

            var adbDevicesRows = devicesCommand.StdOut.ReadToEnd()
                .Replace("\r\n", "\n")
                .Split('\n', '\r');

            devicesCommand.StopProcess();

            if (adbDevicesRows.Length < 2)
            {
                _deviceId = "Not connected to the device.";
                return;
            }

            _deviceId = adbDevicesRows[1].Split('\t')[0];
        }

        private void OnDisable()
        {
            _isRunning = false;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Device Serial", GUILayout.ExpandWidth(false));
            _deviceId = GUILayout.TextField(_deviceId, GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Start"))
            {
                if (_logcatJob == null)
                {
                    Debug.Log("Start");
                    _isRunning = true;
                    _logcatJob = LogcatLoop(_deviceId);
                }
            }

            if (GUILayout.Button("Stop"))
            {
                Debug.Log("Stop");
                _isRunning = false;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            var area = GUILayoutUtility.GetRect(Screen.width, 200f);

            Handles.color = Color.magenta;
            Handles.DrawLine(area.max, area.min);

            GUILayout.Space(10);
        }

        private void Update()
        {
            if (_isRunning) Repaint();
        }

        private async Task LogcatLoop(string deviceId)
        {
            var logcatCommand = new BashCommand(string.Join(" && ",
                Adb.SetPathEnvVar,
                $"adb -s {deviceId} logcat"
            ));

            logcatCommand.StartProcess();

            while (_isRunning)
            {
                Debug.Log($"{await logcatCommand.StdOut.ReadLineAsync()}");
            }

            logcatCommand.StopProcess();

            _logcatJob = null;
        }
    }
}