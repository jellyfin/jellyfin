#pragma warning disable CS1591
#pragma warning disable SA1600

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
        private readonly Process _process;

        private bool _disposed = false;
        private bool _hasExited;

        public CommonProcess(ProcessOptions options)
        {
            StartInfo = options;

            var startInfo = new ProcessStartInfo
            {
                Arguments = options.Arguments,
                FileName = options.FileName,
                WorkingDirectory = options.WorkingDirectory,
                UseShellExecute = options.UseShellExecute,
                CreateNoWindow = options.CreateNoWindow,
                RedirectStandardError = options.RedirectStandardError,
                RedirectStandardInput = options.RedirectStandardInput,
                RedirectStandardOutput = options.RedirectStandardOutput,
                ErrorDialog = options.ErrorDialog
            };


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
                _process.Exited += OnProcessExited;
            }
        }

        public event EventHandler Exited;

        public ProcessOptions StartInfo { get; }

        public StreamWriter StandardInput => _process.StandardInput;

        public StreamReader StandardError => _process.StandardError;

        public StreamReader StandardOutput => _process.StandardOutput;

        public int ExitCode => _process.ExitCode;

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
            // Note: For this function to work correctly, the option EnableRisingEvents needs to be set to true.

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _process?.Dispose();
            }

            _disposed = true;
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            _hasExited = true;
            Exited?.Invoke(this, e);
        }
    }
}
