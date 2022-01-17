using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class BashCommand
    {
        private readonly string _command;
        private Process _process;

        public StreamReader StdOut { get; private set; }
        public StreamReader StdErr { get; private set; }

        public BashCommand(string command)
        {
            _command = command;
        }

        public void StartProcess()
        {
            if (_process != null)
            {
                Debug.LogError("The process is already started.");
                return;
            }

            _process = new Process
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

            _process.Start();

            var writer = _process.StandardInput;
            writer.WriteLine(_command);
            writer.Close();

            StdOut = _process.StandardOutput;
            StdErr = _process.StandardError;
        }

        public void StopProcess()
        {
            StdOut?.Close();
            StdErr?.Close();
            _process?.Close();

            StdOut = null;
            StdErr = null;
            _process = null;
        }
    }
}