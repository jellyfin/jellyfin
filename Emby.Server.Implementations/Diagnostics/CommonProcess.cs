using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Diagnostics;

namespace Emby.Server.Implementations.Diagnostics
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

        private bool _hasExited;
        private bool HasExited
        {
            get
            {
                if (_hasExited)
                {
                    return true;
                }

                try
                {
                    _hasExited = _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    _hasExited = true;
                }

                return _hasExited;
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            _hasExited = true;
            if (Exited != null)
            {
                Exited(this, e);
            }
        }

        public ProcessOptions StartInfo => _options;

        public StreamWriter StandardInput => _process.StandardInput;

        public StreamReader StandardError => _process.StandardError;

        public StreamReader StandardOutput => _process.StandardOutput;

        public int ExitCode => _process.ExitCode;

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

        public Task<bool> WaitForExitAsync(int timeMs)
        {
            //Note: For this function to work correctly, the option EnableRisingEvents needs to be set to true.

            if (HasExited)
            {
                return Task.FromResult(true);
            }

            timeMs = Math.Max(0, timeMs);

            var tcs = new TaskCompletionSource<bool>();

            var cancellationToken = new CancellationTokenSource(timeMs).Token;

            _process.Exited += (sender, args) => tcs.TrySetResult(true);

            cancellationToken.Register(() => tcs.TrySetResult(HasExited));

            return tcs.Task;
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
