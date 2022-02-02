using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Alvr
{
    public interface ITrackingSettingsTarget
    {
        public void ReadSettings(TrackingSettings settings);
        public void ApplySettings(TrackingSettings settings);
    }

    public class TrackingSettings
    {
        private const string MessagePropertyName = "Message";
        private static readonly string[] NewLine = { "\r\n", "\r", "\n" };
        private static readonly char[] KeyValueDelimiter = { '=' };
        private static readonly Regex Vector3Regex = new Regex("[(](.*),(.*),(.*)[)]");

        public string Message { get; set; }

        // TrackingNreal settings
        public float EyeHeight { get; set; }
        public float DiagonalFovAngle { get; set; }
        public float FovRatioInner { get; set; }
        public float FovRatioOuter { get; set; }
        public float FovRatioUpper { get; set; }
        public float FovRatioLower { get; set; }
        public float ZoomRatio { get; set; }
        public float HandUpwardMovement { get; set; }
        public float HandForwardMovement { get; set; }

        // HandTracking settings
        public Vector3 MinAnglePalmFacingFront { get; set; }
        public Vector3 MaxAnglePalmFacingFront { get; set; }
        public float ThresholdAnglePalmFacingBack { get; set; }
        public float ThresholdYDistanceEnableTracking { get; set; }
        public float MinDistance2DInput { get; set; }
        public float MaxDistance2DInput { get; set; }
        public float ThresholdAngleBendThumb { get; set; }
        public float MaxAngleForTrigger { get; set; }
        public float ThresholdAngleForTrigger { get; set; }
        public float MaxAngleForGrip { get; set; }
        public float ThresholdAngleForGrip { get; set; }
        public float SigmaWForAngle { get; set; }
        public float SigmaVForAngle { get; set; }
        public float SigmaWForPosition { get; set; }
        public float SigmaVForPosition { get; set; }

        private readonly Dictionary<string, PropertyInfo> _propertyDict;

        public TrackingSettings()
        {
            _propertyDict = typeof(TrackingSettings).GetProperties().ToDictionary(p => p.Name);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var p in typeof(TrackingSettings).GetProperties())
            {
                builder.AppendLine($"{p.Name}{KeyValueDelimiter[0]}{p.GetValue(this)}");
            }

            return builder.ToString();
        }

        public void CopyFrom(TrackingSettings defaultSettings)
        {
            foreach (var p in typeof(TrackingSettings).GetProperties())
            {
                p.SetValue(this, p.GetValue(defaultSettings));
            }
        }

        public void Parse(string text)
        {
            Message = "";
            foreach (var line in text.Split(NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                var values = line.Split(KeyValueDelimiter);
                if (values.Length != 2)
                {
                    Message += $"'{line}' is invalid format; ";
                    continue;
                }

                var name = values[0].Trim();
                if (name == MessagePropertyName) continue;
                if (!_propertyDict.TryGetValue(name, out var p))
                {
                    Message += $"{name} is invalid name; ";
                    continue;
                }

                var value = ParseValue(p, values[1].Trim());
                if (value != null) p.SetValue(this, value);
            }
        }

        private object ParseValue(PropertyInfo p, string text)
        {
            if (p.PropertyType == typeof(string))
            {
                return text;
            }

            if (p.PropertyType == typeof(float))
            {
                if (float.TryParse(text, out var value)) return value;
                Message += $"{p.Name} must be float; ";
                return null;
            }

            if (p.PropertyType == typeof(Vector3))
            {
                var m = Vector3Regex.Match(text);
                if (!m.Success)
                {
                    Message += $"{p.Name} must be Vector3; ";
                    return null;
                }

                if (float.TryParse(m.Groups[1].Value.Trim(), out var x) &&
                    float.TryParse(m.Groups[2].Value.Trim(), out var y) &&
                    float.TryParse(m.Groups[3].Value.Trim(), out var z))
                {
                    return new Vector3(x, y, z);
                }

                Message += $"{p.Name} values must be float; ";
                return null;
            }

            return null;
        }
    }
}