using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        private bool _doesClearLog;
        private int _dataWindowSize = 1000;
        private string _deviceId;
        private bool _isRunning;
        private Task _logcatJob;
        private Queue<float[]> _timeSeries;
        private List<float> _minValues;
        private List<float> _maxValues;

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

            GUILayout.Label("Data Window Size", GUILayout.ExpandWidth(false));
            int.TryParse(
                GUILayout.TextField(_dataWindowSize.ToString(), GUILayout.ExpandWidth(true)),
                out _dataWindowSize
            );

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Start"))
            {
                if (_logcatJob == null)
                {
                    Debug.Log("Start monitoring");
                    _isRunning = true;
                    _logcatJob = LogcatLoop(_deviceId, _dataWindowSize, _doesClearLog);
                }
                else
                {
                    Debug.Log("Already started");
                }
            }

            if (GUILayout.Button("Stop"))
            {
                Debug.Log("Stop monitoring");
                _isRunning = false;
            }

            GUILayout.EndHorizontal();

            _doesClearLog = GUILayout.Toggle(_doesClearLog, "Clear the log at the start");

            DrawGraph(position.size.x, 200f, 10, 10);
        }

        private void DrawGraph(float width, float height, float verticalSpace, float horizontalSpace)
        {
            GUILayout.Space(verticalSpace);

            var area = GUILayoutUtility.GetRect(
                width, height, GUILayout.Width(width), GUILayout.Height(height));

            var areaPoints = new Vector3[]
            {
                area.min,
                new Vector2(area.xMin, area.yMax),
                area.max,
                new Vector2(area.xMax, area.yMin),
                new Vector2(area.xMin, area.yMin + height / 4),
                new Vector2(area.xMax, area.yMin + height / 4),
                new Vector2(area.xMin, area.yMin + height / 4 * 2),
                new Vector2(area.xMax, area.yMin + height / 4 * 2),
                new Vector2(area.xMin, area.yMin + height / 4 * 3),
                new Vector2(area.xMax, area.yMin + height / 4 * 3)
            };
            var areaLineIndexes = new[]
            {
                0, 1,
                1, 2,
                2, 3,
                3, 0,
                6, 7,
            };
            var areaDottedLineIndexes = new[]
            {
                4, 5,
                8, 9
            };

            Handles.color = Color.white;
            Handles.DrawLines(areaPoints, areaLineIndexes);
            Handles.DrawDottedLines(areaPoints, areaDottedLineIndexes, width / _dataWindowSize / 2);

            if (_timeSeries == null || _timeSeries.Count < 2)
            {
                GUILayout.Space(verticalSpace);
                return;
            }

            var timeSeries = _timeSeries.ToArray();
            var dataPoints = timeSeries.SelectMany(
                (timePoint, t) => timePoint.Select(
                    (value, l) => new Vector3(
                        area.xMin + width * t / _dataWindowSize,
                        area.yMax - height * ToRatio(value, _minValues[l], _maxValues[l]),
                        0f
                    )
                )
            ).ToArray();

            var lineNum = dataPoints.Length / timeSeries.Length;
            var dataIndexes = Enumerable.Range(0, timeSeries.Length - 1).SelectMany(
                t => Enumerable.Range(t * lineNum, lineNum).Select(
                    l => new[] { l, l + lineNum }
                )
            ).GroupBy(lineIndexes => lineIndexes[0] % lineNum);

            foreach (var group in dataIndexes)
            {
                var indexed = @group.SelectMany(i => i).ToArray();
                Handles.color = Color.HSVToRGB((float)group.Key / lineNum, 1f, 1f);
                Handles.DrawLines(dataPoints, indexed);
            }

            GUILayout.Space(verticalSpace);

            const float legendLineSize = 20f;
            const float textWidth = 100f;

            GUILayout.BeginHorizontal();

            GUILayout.Space(horizontalSpace);
            GUILayoutUtility.GetRect(legendLineSize, legendLineSize, GUILayout.ExpandWidth(false));
            GUILayout.Space(horizontalSpace);
            GUILayout.Label("min.", GUILayout.Width(textWidth));
            GUILayout.Label("max.", GUILayout.Width(textWidth));

            GUILayout.EndHorizontal();

            for (var l = 0; l < lineNum; l++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(horizontalSpace);

                var legendLine = GUILayoutUtility.GetRect(legendLineSize, legendLineSize, GUILayout.ExpandWidth(false));
                Handles.color = Color.HSVToRGB((float)l / lineNum, 1f, 1f);
                Handles.DrawLine(
                    new Vector2(legendLine.xMin, legendLine.center.y),
                    new Vector2(legendLine.xMax, legendLine.center.y)
                );

                GUILayout.Space(horizontalSpace);

                if (float.TryParse(GUILayout.TextField(_minValues[l].ToString(CultureInfo.InvariantCulture),
                        GUILayout.Width(textWidth)), out var minValue))
                {
                    _minValues[l] = minValue;
                }

                if (float.TryParse(GUILayout.TextField(_maxValues[l].ToString(CultureInfo.InvariantCulture),
                        GUILayout.Width(textWidth)), out var maxValue))
                {
                    _maxValues[l] = maxValue;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(verticalSpace);
        }

        private void Update()
        {
            if (_isRunning) Repaint();
        }

        private async Task LogcatLoop(string deviceId, int dataWindowSize, bool doesClearLog)
        {
            _timeSeries = new Queue<float[]>(dataWindowSize);
            _minValues = new List<float>();
            _maxValues = new List<float>();

            const string keyword = "TimeSeriesData";

            var logcatCommand = new BashCommand(string.Join(" && ",
                Adb.SetPathEnvVar,
                doesClearLog ? $"adb -s {deviceId} logcat -c" : "true",
                $"adb -s {deviceId} logcat -b main -e '.*{keyword}.*'"
            ));

            logcatCommand.StartProcess();

            var delimiter = new[] { ' ' };

            while (_isRunning)
            {
                var line = await logcatCommand.StdOut.ReadLineAsync();
                var data = line.Substring(line.IndexOf(keyword, StringComparison.Ordinal) + keyword.Length);
                try
                {
                    var values = data.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                        .Select(float.Parse).ToArray();
                    if (_timeSeries.Count == dataWindowSize) _timeSeries.Dequeue();
                    _timeSeries.Enqueue(values);

                    for (var i = _minValues.Count; i < values.Length; i++) _minValues.Add(0);
                    for (var i = _maxValues.Count; i < values.Length; i++) _maxValues.Add(0);

                    for (var i = 0; i < values.Length; i++)
                    {
                        if (values[i] < _minValues[i]) _minValues[i] = values[i];
                        if (values[i] > _maxValues[i]) _maxValues[i] = values[i];
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.Message} : {line}");
                }
            }

            logcatCommand.StopProcess();

            _logcatJob = null;
        }

        private static float ToRatio(float value, float minValue, float maxValue)
        {
            return (value - minValue) / (maxValue - minValue);
        }
    }
}