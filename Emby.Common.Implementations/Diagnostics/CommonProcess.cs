using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Diagnostics;

namespace Emby.Common.Implementations.Diagnostics
{
    public class CommonProcess : IProcess
    {
        public event EventHandler Exited;

        private readonly ProcessOptions _options;
        private readonly Process _process;

        public CommonProcess(ProcessOptions options)
        {
            _options = options;

            var startInfo = new ProcessStartInfo
            {
                Arguments = options.Arguments,
                FileName = options.FileName,
                WorkingDirectory = options.WorkingDirectory,
                UseShellExecute = options.UseShellExecute,
                CreateNoWindow = options.CreateNoWindow,
                RedirectStandardError = options.RedirectStandardError,
                RedirectStandardInput = options.RedirectStandardInput,
                RedirectStandardOutput = options.RedirectStandardOutput
            };

            startInfo.ErrorDialog = options.ErrorDialog;

            if (options.IsHidden)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }

            _process = new Process
            {
                StartInfo = startInfo
            };

            if (options.EnableRaisingEvents)
            {
                _process.EnableRaisingEvents = true;
                _process.Exited += _process_Exited;
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            if (Exited != null)
            {
                Exited(this, e);
            }
        }

        public ProcessOptions StartInfo
        {
            get { return _options; }
        }

        public StreamWriter StandardInput
        {
            get { return _process.StandardInput; }
        }

        public StreamReader StandardError
        {
            get { return _process.StandardError; }
        }

        public StreamReader StandardOutput
        {
            get { return _process.StandardOutput; }
        }

        public int ExitCode
        {
            get { return _process.ExitCode; }
        }

        public void Start()
        {
            _process.Start();
        }

        public void Kill()
        {
            _process.Kill();
        }

        public bool WaitForExit(int timeMs)
        {
            return _process.WaitForExit(timeMs);
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
