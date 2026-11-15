using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Owns compatibility processes even while their asynchronous caller is still reading output.
/// Closing the owner prevents a late launch from being accepted after server shutdown.
/// </summary>
internal sealed class TranscodeTestProcessOwner : IDisposable
{
    private readonly object _lock = new();
    private readonly HashSet<Process> _processes = [];
    private bool _closed;

    internal bool TryAdd(Process process)
    {
        lock (_lock)
        {
            if (_closed)
            {
                return false;
            }

            return _processes.Add(process);
        }
    }

    internal void Remove(Process process)
    {
        lock (_lock)
        {
            _processes.Remove(process);
        }
    }

    public void Dispose()
    {
        // The asynchronous caller owns disposal; do not race its stderr reader by disposing the Process here.
        lock (_lock)
        {
            _closed = true;
            foreach (var process in _processes)
            {
                Kill(process);
            }

            var deadline = Stopwatch.StartNew();
            foreach (var process in _processes)
            {
                WaitForExit(process, TimeSpan.FromSeconds(5) - deadline.Elapsed);
            }

            _processes.Clear();
        }
    }

    internal static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // An exit racing shutdown is harmless; waiting below still bounds cleanup.
        }
    }

    internal static void WaitForExit(Process process, TimeSpan wait)
    {
        if (wait <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            process.WaitForExit((int)Math.Min(wait.TotalMilliseconds, int.MaxValue));
        }
        catch (InvalidOperationException)
        {
            // A late or failed launch may not have associated a process handle.
        }
    }
}
