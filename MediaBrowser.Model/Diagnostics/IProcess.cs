using System;
using System.IO;

namespace MediaBrowser.Model.Diagnostics
{
    public interface IProcess : IDisposable
    {
        event EventHandler Exited;

        void Kill();
        bool WaitForExit(int timeMs);
        int ExitCode { get; }
        void Start();
        StreamWriter StandardInput { get; }
        StreamReader StandardError { get; }
        StreamReader StandardOutput { get; }
        ProcessOptions StartInfo { get; }
    }
}
