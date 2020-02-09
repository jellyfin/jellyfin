#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Diagnostics
{
    public interface IProcess : IDisposable
    {
        event EventHandler Exited;

        void Kill();
        bool WaitForExit(int timeMs);
        Task<bool> WaitForExitAsync(int timeMs);
        int ExitCode { get; }
        void Start();
        StreamWriter StandardInput { get; }
        StreamReader StandardError { get; }
        StreamReader StandardOutput { get; }
        ProcessOptions StartInfo { get; }
    }
}
