// ReSharper disable All (the class for TimeSeriesDataViewer)

using System.Text;
using UnityEngine;

namespace Alvr
{
    public static class TimeSeriesData
    {
        public static void Log(params float[] values)
        {
            var builder = new StringBuilder("TimeSeriesData");
            for (int i = 0; i < values.Length; i++)
            {
                builder.Append(' ');
                builder.Append(values[i]);
            }
            Debug.Log(builder.ToString());
        }
    }
}