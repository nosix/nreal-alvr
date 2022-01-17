using System.IO;
using UnityEditor;

namespace Editor
{
    public static class Adb
    {
        private static string GetPath()
        {
            var applicationRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
            var adbCommand = Path.Combine(applicationRoot!, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb");
            return Path.GetDirectoryName(adbCommand);
        }

        public static string SetPathEnvVar => $@"PATH={GetPath()}:$PATH";
    }
}