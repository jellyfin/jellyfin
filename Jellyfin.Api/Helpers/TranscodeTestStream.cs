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

    /// <summary>
    /// Seconds before the end of the source, at the stream's speed, in which it is no longer judged. ffmpeg drains and
    /// flushes at the end of the file, which can read as a dip; the stream then restarts and is judged afresh.
    /// </summary>
    internal const double EndMarginSeconds = TranscodeTestManager.FailSeconds + WindowSeconds;

    private const int TailLines = 5;
    private const int ErrorLineCount = 4;
    private readonly object _lock = new();
    private readonly Queue<(double Time, long Frame)> _points = new();
    private readonly Queue<string> _tail = new();
    private readonly Queue<string> _errors = new();
    private readonly List<double> _samples = [];
    private readonly HashSet<string> _counted = new(StringComparer.Ordinal);
    private long _outputBytes;
    private int _outputSegments;
    private bool _disposed;
    private bool _exited;
    private int _exitCode = -1;
    private Task _reader = Task.CompletedTask;
    private double? _position;
    private string? _sessionLimitLine;
    private string? _gpuMemoryLine;
    private bool _reportedFrames;
    private double? _firstFrameAt;
    private double? _lastLineAt;
    private double? _inputAt;
    private bool _startedBefore;
    private bool _sawMapping;
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

    /// <summary>
    /// Gets a value indicating whether the output reader finished, without blocking the supervisor.
    /// </summary>
    public bool OutputCompleted
    {
        get
        {
            lock (_lock)
            {
                return _reader.IsCompleted;
            }
        }
    }

    public int Id { get; }

    public Process Process { get; }

    public string Folder { get; }

    public string FilePrefix { get; }

    public float SourceFps { get; }

    public double StartedAt { get; }

    public int SegmentLength { get; }

    /// <summary>
    /// Gets the source's run time in seconds, or null when it is not known.
    /// </summary>
    public double? SourceSeconds { get; init; }

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

    /// <summary>
    /// Gets or sets how many times this stream was started again at the end of the file before this process.
    /// </summary>
    public int Restarts { get; set; }

    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the time to the first frame was logged.
    /// </summary>
    public bool FirstFrameLogged { get; set; }

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
    /// Gets a value indicating whether the stream is so close to the end of its source, at its current speed, that it
    /// is not judged: see <see cref="EndMarginSeconds"/>.
    /// </summary>
    public bool NearEnd
    {
        get
        {
            lock (_lock)
            {
                return NearEndLocked();
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

    /// <summary>
    /// Gets what ffmpeg last said, for a stream that never produced frames: its error lines, or else its last few
    /// lines of any kind. Empty when it printed nothing.
    /// </summary>
    public string StallEvidence
    {
        get
        {
            lock (_lock)
            {
                return SelectEvidence(string.Join(" | ", _errors), _tail.ToArray());
            }
        }
    }

    /// <summary>
    /// Gets the first line in which ffmpeg printed NVIDIA's session refusal text, or null. Kept for the stream's life,
    /// however many lines follow it.
    /// </summary>
    public string? SessionLimitLine
    {
        get
        {
            lock (_lock)
            {
                return _sessionLimitLine;
            }
        }
    }

    /// <summary>
    /// Gets the first line in which ffmpeg said the GPU ran out of memory, or null. Kept for the stream's life.
    /// </summary>
    public string? GpuMemoryLine
    {
        get
        {
            lock (_lock)
            {
                return _gpuMemoryLine;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether ffmpeg reported at least one encoded frame.
    /// </summary>
    public bool ReportedFrames
    {
        get
        {
            lock (_lock)
            {
                return _reportedFrames;
            }
        }
    }

    /// <summary>
    /// Gets when ffmpeg reported the first encoded frame, in seconds since the test started, or null before that.
    /// </summary>
    public double? FirstFrameAt
    {
        get
        {
            lock (_lock)
            {
                return _firstFrameAt;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether ffmpeg opened the input, which it says with "Input #0" once it has read enough
    /// of the file to know its streams.
    /// </summary>
    public bool SawInput
    {
        get
        {
            lock (_lock)
            {
                return _inputAt.HasValue;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether ffmpeg printed no line at all.
    /// </summary>
    /// <summary>
    /// Gets ffmpeg's last reported position in the source, in seconds, see <see cref="ProgressPosition"/>. Null before
    /// its first progress line. <see cref="NearEnd"/> uses the same value.
    /// </summary>
    public double? PositionSeconds
    {
        get
        {
            lock (_lock)
            {
                return _position;
            }
        }
    }

    /// <summary>
    /// Gets whether the process's last position was at the end of the source, for a clean exit: see
    /// <see cref="TranscodeTestRules.IsAtEndOfFile"/>. Null when the run time or the position is not known. It reads
    /// the same position as <see cref="NearEnd"/>, but not its margin: at a high speed that margin covers most of a
    /// short file, and a stream that stops halfway did not reach the end.
    /// </summary>
    public bool? AtEndOfFile
    {
        get
        {
            lock (_lock)
            {
                return TranscodeTestRules.IsAtEndOfFile(_position, SourceSeconds);
            }
        }
    }

    public bool PrintedNothing
    {
        get
        {
            lock (_lock)
            {
                return _lastLineAt is null;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the stream produced frames, in this process or in one before it that reached
    /// the end of the file.
    /// </summary>
    public bool EverReportedFrames
    {
        get
        {
            lock (_lock)
            {
                return _reportedFrames || _startedBefore;
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
    /// <param name="waitForOutput">Whether to wait briefly for the reader; false after the supervisor already drained it.</param>
    public void DisposeProcess(bool waitForOutput = true)
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

        if (waitForOutput && HasExited)
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

    private bool NearEndLocked()
    {
        if (SourceSeconds is not double length || length <= 0 || _position is not double position)
        {
            return false;
        }

        var remaining = length - position;
        if (remaining <= 0)
        {
            return true;
        }

        // A stream faster than real time reaches the end sooner, so the margin is in the stream's own time.
        var speed = 1.0;
        if (_points.Count >= 2 && SourceFps > 0)
        {
            var (t0, f0) = _points.First();
            var (t1, f1) = _points.Last();
            if (t1 > t0)
            {
                speed = Math.Max(speed, (f1 - f0) / (t1 - t0) / SourceFps);
            }
        }

        return remaining <= EndMarginSeconds * speed;
    }

    /// <summary>
    /// Gets the frame rate over the last few seconds.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    /// <returns>The frame rate; 0 when ffmpeg reported frames but then went silent; <c>null</c> before it can be
    /// measured, which is until a full window has passed since the first frame, so the encoder's start is not
    /// judged, and near the end of the source, see <see cref="NearEnd"/>.</returns>
    public double? Fps(double now)
    {
        lock (_lock)
        {
            if (_points.Count == 0)
            {
                return null;
            }

            // ffmpeg drains and flushes at the end of the file: not a dip, as the stream restarts and is judged afresh.
            if (NearEndLocked())
            {
                return null;
            }

            var (t0, f0) = _points.First();
            var (t1, f1) = _points.Last();
            if (now - t1 > Math.Max(WindowSeconds, 2))
            {
                // ffmpeg prints a progress line twice a second while it runs, so a started stream this quiet has stopped.
                return _reportedFrames ? 0 : null;
            }

            // A new process, also one started again at the end of the file, warms up first: only readings from a
            // full window after its first frame count.
            if (_firstFrameAt is not double first || now - first < WindowSeconds || t0 < first)
            {
                return null;
            }

            if (_points.Count < 2 || t1 - t0 < WindowSeconds / 2)
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
    /// Describes how long the stream took to its first frame, split at the time ffmpeg opened the file when it said so.
    /// </summary>
    /// <param name="bytesRead">The bytes ffmpeg has read, or null when they are not known.</param>
    /// <returns>One line for the log, or null before the first frame.</returns>
    public string? DescribeFirstFrame(long? bytesRead)
    {
        lock (_lock)
        {
            if (_firstFrameAt is not double first)
            {
                return null;
            }

            var read = bytesRead is long b ? $" ({TranscodeTestProcessIo.FormatBytes(b)} read)" : string.Empty;
            return _inputAt is double input && input <= first
                ? string.Create(CultureInfo.InvariantCulture, $"Stream {Id}: opened the file within {input - StartedAt:0.0} s; first frame {first - input:0.0} s later{read}.")
                : string.Create(CultureInfo.InvariantCulture, $"Stream {Id}: first frame within {first - StartedAt:0.0} s{read}.");
        }
    }

    /// <summary>
    /// Describes how far a stream that produced no frames got: whether ffmpeg opened the file and reached its stream
    /// mapping, how long it has been quiet, how much CPU time it used and how much it read. One short line when ffmpeg
    /// printed nothing.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    /// <param name="processorTime">ffmpeg's CPU time, or null when it could not be read.</param>
    /// <param name="bytesRead">The bytes ffmpeg has read, or null when they are not known.</param>
    /// <returns>One line for the log.</returns>
    public string StallDiagnostics(double now, TimeSpan? processorTime, long? bytesRead)
    {
        lock (_lock)
        {
            var cpu = processorTime is TimeSpan p ? string.Create(CultureInfo.InvariantCulture, $"CPU time {p.TotalSeconds:0.0} s") : "CPU time unknown";
            var read = bytesRead is long b ? $"{TranscodeTestProcessIo.FormatBytes(b)} read" : null;
            if (_lastLineAt is not double t)
            {
                var usage = read is null ? cpu : cpu + ", " + read;
                return string.Create(CultureInfo.InvariantCulture, $"Stream {Id} printed nothing in {now - StartedAt:0.0} s ({usage}).");
            }

            var details = string.Create(
                CultureInfo.InvariantCulture,
                $"Stream {Id} after {now - StartedAt:0.0} s: opened the file: {(_inputAt.HasValue ? "yes" : "no")}; reached stream mapping: {(_sawMapping ? "yes" : "no")}; last output {now - t:0.0} s ago; {cpu}");
            return read is null ? details + "." : details + "; " + read + ".";
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
        bool started;
        lock (previous._lock)
        {
            bytes = previous._outputBytes;
            segments = previous._outputSegments;
            samples = previous._samples.ToArray();
            started = previous._reportedFrames || previous._startedBefore;
        }

        lock (_lock)
        {
            _startedBefore |= started;
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

    internal void OnLine(string line, double now)
    {
        // The stress command adds "-loglevel +level", so ffmpeg tags each line with its level; the tag is removed here.
        (var level, line) = SplitLevel(line);
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
                _lastLineAt = now;
                _position = ProgressPosition(OutputSeconds(line), frame, SourceFps);
                if (frame >= 1 && !_reportedFrames)
                {
                    _reportedFrames = true;
                    _firstFrameAt = now;
                }

                while (_points.Count > 2 && now - _points.ElementAt(1).Time >= WindowSeconds)
                {
                    _points.Dequeue();
                }
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(line))
        {
            var trimmed = line.Trim();
            lock (_lock)
            {
                _lastLineAt = now;
                if (_inputAt is null && trimmed.StartsWith("Input #0", StringComparison.Ordinal))
                {
                    _inputAt = now;
                }

                _sawMapping |= trimmed.StartsWith("Stream mapping:", StringComparison.Ordinal);
                // Checked as each line arrives: the kept error text below may lose the line that names the cause.
                if (_sessionLimitLine is null && TranscodeTestRules.IsSessionLimitText(trimmed))
                {
                    _sessionLimitLine = trimmed;
                }
                else if (_gpuMemoryLine is null && TranscodeTestRules.IsGpuMemoryLine(trimmed))
                {
                    _gpuMemoryLine = trimmed;
                }

                _tail.Enqueue(trimmed);
                while (_tail.Count > TailLines)
                {
                    _tail.Dequeue();
                }
            }
        }

        if (IsErrorLine(level, line))
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

    /// <summary>
    /// Gets how far into the source a progress line is, in seconds: the later of its time and its frame count at the
    /// source frame rate. With playback's "-copyts" HLS command the time lags the frames encoded, by up to a few
    /// seconds at the end of the file: a 13.5 s file ends at "frame=321 … time=00:00:10.35" with every frame done.
    /// The frame count is what the test measures against the source frame rate already.
    /// </summary>
    /// <param name="outputSeconds">The line's time, see <see cref="OutputSeconds"/>, or null when it has none.</param>
    /// <param name="frame">The line's frame count.</param>
    /// <param name="sourceFps">The source frame rate, or 0 when it is not known.</param>
    /// <returns>The seconds, or null when the line gives neither.</returns>
    internal static double? ProgressPosition(double? outputSeconds, long frame, float sourceFps)
    {
        double? fromFrames = sourceFps > 0 ? frame / (double)sourceFps : null;
        return outputSeconds is double time && fromFrames is double frames ? Math.Max(time, frames) : outputSeconds ?? fromFrames;
    }

    /// <summary>
    /// Gets how far ffmpeg's output is from a progress line, in seconds of the source.
    /// </summary>
    /// <param name="line">A progress line, such as "frame=  120 fps= 60 … time=00:00:05.00 …".</param>
    /// <returns>The seconds, or null when the line has no time, as before the first frame ("time=N/A").</returns>
    internal static double? OutputSeconds(string line)
    {
        var match = TimeRegex().Match(line);
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            || !double.TryParse(match.Groups[3].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
        {
            return null;
        }

        return (hours * 3600) + (minutes * 60) + seconds;
    }

    /// <summary>
    /// Splits ffmpeg's level tag from a line printed with "-loglevel +level".
    /// </summary>
    /// <param name="line">One line of ffmpeg output, such as "[h264_nvenc @ 0x0] [error] OpenEncodeSessionEx failed".</param>
    /// <returns>The level, or null when the line has no tag, and the line without the tag.</returns>
    internal static (string? Level, string Text) SplitLevel(string line)
    {
        var match = LevelRegex().Match(line);
        return match.Success ? (match.Groups[2].Value, match.Groups[1].Value + line[match.Length..]) : (null, line);
    }

    /// <summary>
    /// Gets whether a line is one of ffmpeg's errors.
    /// </summary>
    /// <remarks>
    /// A tagged line is an error at error level or worse, so warnings such as a remux's "Invalid Block Addition value"
    /// are never reported as the cause. Some libraries print straight to stderr without a tag; those fall back to
    /// matching words that errors contain.
    /// </remarks>
    /// <param name="level">The line's level, or null when it has no tag.</param>
    /// <param name="text">The line without its tag.</param>
    /// <returns><c>true</c> for an error.</returns>
    internal static bool IsErrorLine(string? level, string text)
        => level is null
            ? text.Contains("rror", StringComparison.Ordinal) || text.Contains("ailed", StringComparison.Ordinal) || text.Contains("nvalid", StringComparison.Ordinal)
            : level is "panic" or "fatal" or "error";

    /// <summary>
    /// Picks the evidence for a stalled stream: its error lines, or else its last few lines.
    /// </summary>
    /// <param name="lastError">The stream's error lines.</param>
    /// <param name="tail">Its last lines that were not progress lines.</param>
    /// <returns>The evidence, or an empty string.</returns>
    internal static string SelectEvidence(string lastError, IEnumerable<string> tail)
        => !string.IsNullOrWhiteSpace(lastError) ? lastError : string.Join(" | ", tail);

    [GeneratedRegex(@"^frame=\s*(\d+)")]
    private static partial Regex FrameRegex();

    // How far the output is, in the source's time: "time=00:01:05.04"; "time=N/A" before the first frame.
    [GeneratedRegex(@"\btime=(\d+):(\d{2}):(\d{2}(?:\.\d+)?)")]
    private static partial Regex TimeRegex();

    // ffmpeg prints the level after any context prefixes: "[ctx @ 0x0] [sub @ 0x0] [error] message" or "[info] message".
    [GeneratedRegex(@"^((?:\[[^\[\]]*\] )*)\[(panic|fatal|error|warning|info|verbose|debug|trace)\] ")]
    private static partial Regex LevelRegex();
}
