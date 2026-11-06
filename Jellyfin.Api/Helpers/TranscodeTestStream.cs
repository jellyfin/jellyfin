using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// One ffmpeg process of the capacity test and its live statistics.
/// </summary>
internal sealed partial class TranscodeTestStream
{
    /// <summary>
    /// Seconds over which a stream's frame rate is measured.
    /// </summary>
    internal const double WindowSeconds = 3;

    private const int ErrorLineCount = 4;
    private readonly object _lock = new();
    private readonly Queue<(double Time, long Frame)> _points = new();
    private readonly Queue<string> _errors = new();
    private readonly List<double> _samples = [];
    private readonly HashSet<string> _counted = new(StringComparer.Ordinal);
    private long _outputBytes;
    private int _outputSegments;
    private bool _disposed;
    private bool _exited;
    private int _exitCode = -1;
    private Task _reader = Task.CompletedTask;
    private bool _stopping;

    public TranscodeTestStream(int id, Process process, string folder, string filePrefix, float sourceFps, double startedAt, int segmentLength)
    {
        Id = id;
        Process = process;
        Folder = folder;
        FilePrefix = filePrefix;
        SourceFps = sourceFps;
        StartedAt = startedAt;
        SegmentLength = segmentLength;
    }

    public int Id { get; }

    public Process Process { get; }

    public string Folder { get; }

    public string FilePrefix { get; }

    public float SourceFps { get; }

    public double StartedAt { get; }

    public int SegmentLength { get; }

    /// <summary>
    /// Gets a value indicating whether the test stopped the process on purpose.
    /// </summary>
    public bool Stopping
    {
        get
        {
            lock (_lock)
            {
                return _stopping;
            }
        }
    }

    public bool Handled { get; set; }

    /// <summary>
    /// Gets a value indicating whether the process has exited. Its exit code is kept when it is seen, so this and
    /// <see cref="ExitCode"/> can be read from any thread, also after the process was released.
    /// </summary>
    public bool HasExited
    {
        get
        {
            lock (_lock)
            {
                return RefreshExitLocked();
            }
        }
    }

    /// <summary>
    /// Gets or sets the frame rate when the test ended.
    /// </summary>
    public double? FinalFps { get; set; }

    /// <summary>
    /// Gets ffmpeg's exit code, or -1 while it runs or when it could not be read.
    /// </summary>
    public int ExitCode
    {
        get
        {
            lock (_lock)
            {
                return RefreshExitLocked() ? _exitCode : -1;
            }
        }
    }

    /// <summary>
    /// Gets ffmpeg's last error lines, whole and in order, joined with " | ". Empty when it printed none.
    /// </summary>
    public string LastError => string.Join(" | ", ErrorLines);

    /// <summary>
    /// Gets ffmpeg's last few error lines, whole and in order, with a line identical to the one before it left out.
    /// </summary>
    public IReadOnlyList<string> ErrorLines
    {
        get
        {
            lock (_lock)
            {
                return _errors.ToArray();
            }
        }
    }

    public double? OutputMbps
    {
        get
        {
            lock (_lock)
            {
                return _outputSegments > 0 ? Math.Round(_outputBytes * 8.0 / (_outputSegments * SegmentLength) / 1e6, 2) : null;
            }
        }
    }

    public double? MedianFps
    {
        get
        {
            lock (_lock)
            {
                if (_samples.Count == 0)
                {
                    return null;
                }

                var sorted = _samples.Order().ToList();
                return Math.Round(sorted[sorted.Count / 2], 1);
            }
        }
    }

    /// <summary>
    /// Reads ffmpeg's stats lines (frame=…), which it ends with carriage returns.
    /// </summary>
    /// <param name="clock">The test's clock.</param>
    /// <returns>A task that completes when ffmpeg closes its error output.</returns>
    public async Task ReadProgressAsync(Stopwatch clock)
    {
        var line = new StringBuilder();
        var buffer = new char[4096];
        try
        {
            var reader = Process.StandardError;
            int read;
            while ((read = await reader.ReadAsync(buffer).ConfigureAwait(false)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    var c = buffer[i];
                    if (c is '\r' or '\n')
                    {
                        OnLine(line.ToString(), clock.Elapsed.TotalSeconds);
                        line.Clear();
                    }
                    else
                    {
                        line.Append(c);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // The process was released while its output was read; ObjectDisposedException is an InvalidOperationException.
        }
    }

    /// <summary>
    /// Starts reading ffmpeg's output in the background.
    /// </summary>
    /// <param name="clock">The test's clock.</param>
    public void StartReading(Stopwatch clock)
    {
        var reader = Task.Run(() => ReadProgressAsync(clock), CancellationToken.None);
        lock (_lock)
        {
            _reader = reader;
        }
    }

    /// <summary>
    /// Waits until ffmpeg's output has been read to its end, so an exited stream is judged on all it printed.
    /// </summary>
    /// <param name="timeout">How long to wait at most.</param>
    /// <returns>A task that completes when the output was read or the timeout passed.</returns>
    public async Task WaitForOutputAsync(TimeSpan timeout)
    {
        Task reader;
        lock (_lock)
        {
            reader = _reader;
        }

        try
        {
            await reader.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
    }

    /// <summary>
    /// Stops the process and its children at once. The output is thrown away, so ffmpeg need not finish its files.
    /// Safe to call more than once and from any thread.
    /// </summary>
    public void Kill()
    {
        lock (_lock)
        {
            _stopping = true;
            if (RefreshExitLocked())
            {
                return;
            }

            try
            {
                Process.Kill(true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                // It exited meanwhile, or it cannot be stopped; the wait below then gives up.
            }
        }
    }

    /// <summary>
    /// Waits for the process to exit, up to a time limit.
    /// </summary>
    /// <param name="timeout">How long to wait at most.</param>
    /// <returns><c>true</c> when it exited.</returns>
    public bool WaitForExit(TimeSpan timeout)
    {
        lock (_lock)
        {
            if (RefreshExitLocked())
            {
                return true;
            }
        }

        try
        {
            return Process.WaitForExit(timeout);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // Released meanwhile.
            return true;
        }
    }

    /// <summary>
    /// Waits for the process to exit.
    /// </summary>
    /// <param name="cancellationToken">Ends the wait.</param>
    /// <returns>A task that completes when it exited or the wait was cancelled.</returns>
    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (HasExited)
        {
            return;
        }

        try
        {
            await Process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
        }
    }

    /// <summary>
    /// Releases the process, keeping its exit code. Its output reader is given a moment to finish first. Safe to
    /// call more than once and from any thread.
    /// </summary>
    public void DisposeProcess()
    {
        Task reader;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            reader = _reader;
        }

        if (HasExited)
        {
            // The output closes with the process, so this returns quickly; it is bounded in case a child kept it open.
            reader.Wait(TimeSpan.FromSeconds(1));
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            RefreshExitLocked();
            _exited = true;
            _disposed = true;
            Process.Dispose();
        }
    }

    // Reads the exit once, under the lock, and keeps it, so a released process is never asked again.
    private bool RefreshExitLocked()
    {
        if (_exited || _disposed)
        {
            return true;
        }

        try
        {
            if (!Process.HasExited)
            {
                return false;
            }

            _exitCode = Process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            // Not started, or no longer associated with a process.
            _exitCode = -1;
        }

        _exited = true;
        return true;
    }

    public double? Fps(double now)
    {
        lock (_lock)
        {
            if (_points.Count < 2)
            {
                return null;
            }

            var (t0, f0) = _points.First();
            var (t1, f1) = _points.Last();
            if (t1 - t0 < WindowSeconds / 2 || now - t1 > Math.Max(WindowSeconds, 2))
            {
                return null;
            }

            return (f1 - f0) / (t1 - t0);
        }
    }

    public void Sample(double now)
    {
        var fps = Fps(now);
        lock (_lock)
        {
            if (fps.HasValue)
            {
                _samples.Add(fps.Value);
            }
        }
    }

    /// <summary>
    /// Takes over the output and frame rate samples of the process this one replaces, when a stream is started again
    /// at the end of the file, so the stream's output bitrate and median frame rate cover its whole run.
    /// </summary>
    /// <param name="previous">The process that reached the end of the file.</param>
    public void CarryOver(TranscodeTestStream previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        long bytes;
        int segments;
        double[] samples;
        lock (previous._lock)
        {
            bytes = previous._outputBytes;
            segments = previous._outputSegments;
            samples = previous._samples.ToArray();
        }

        lock (_lock)
        {
            _outputBytes += bytes;
            _outputSegments += segments;
            _samples.InsertRange(0, samples);
        }
    }

    /// <summary>
    /// Counts finished segments for the output bitrate and deletes all but the newest few, so a long test does not fill the disk.
    /// </summary>
    /// <param name="exited">Whether ffmpeg has exited, so the newest segment is finished too.</param>
    public void CollectSegments(bool exited = false)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(Folder, FilePrefix + "*")
                .Where(f => f.EndsWith(".ts", StringComparison.Ordinal) || f.EndsWith(".mp4", StringComparison.Ordinal))
                .Select(f => (Path: f, Number: SegmentNumber(f)))
                .Where(f => f.Number >= 0)
                .OrderBy(f => f.Number)
                .Select(f => f.Path)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        // The newest segment may still be being written while ffmpeg runs.
        var finished = exited || files.Length == 0 ? files : files[..^1];
        lock (_lock)
        {
            foreach (var file in finished.Where(f => !_counted.Contains(f)))
            {
                try
                {
                    _outputBytes += new FileInfo(file).Length;
                    _outputSegments++;
                    _counted.Add(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
        }

        foreach (var file in finished.SkipLast(3))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    // Segments are named <prefix><number>.ts; the fMP4 header is <prefix>-1.mp4.
    private int SegmentNumber(string path)
        => int.TryParse(Path.GetFileNameWithoutExtension(path).AsSpan(FilePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : -1;

    private void OnLine(string line, double now)
    {
        var match = FrameRegex().Match(line);
        if (match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frame))
        {
            lock (_lock)
            {
                if (_points.Count > 0 && frame < _points.Last().Frame)
                {
                    _points.Clear();
                }

                _points.Enqueue((now, frame));
                while (_points.Count > 2 && now - _points.ElementAt(1).Time >= WindowSeconds)
                {
                    _points.Dequeue();
                }
            }

            return;
        }

        if (line.Contains("rror", StringComparison.Ordinal) || line.Contains("ailed", StringComparison.Ordinal) || line.Contains("nvalid", StringComparison.Ordinal))
        {
            var error = line.Trim();
            lock (_lock)
            {
                // Keep whole lines, so the shown error never starts mid-line, and leave out a repeat of the line before.
                if (_errors.Count == 0 || !string.Equals(_errors.Last(), error, StringComparison.Ordinal))
                {
                    _errors.Enqueue(error);
                    while (_errors.Count > ErrorLineCount)
                    {
                        _errors.Dequeue();
                    }
                }
            }
        }
    }

    [GeneratedRegex(@"^frame=\s*(\d+)")]
    private static partial Regex FrameRegex();
}
