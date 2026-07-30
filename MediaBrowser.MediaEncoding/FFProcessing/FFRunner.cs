using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.FFProcessing;

/// <summary>
/// Starts an FFmpeg or FFprobe process, supervises it, and kills the tree on cancellation.
/// </summary>
public sealed class FFRunner : IFFRunner, IDisposable
{
    /// <summary>
    /// Starting capacity for the rendered arguments. Measured against real invocations, this covers
    /// everything but the longest filter chains without a regrow, and costs 1 KB transiently per spawn.
    /// </summary>
    private const int ArgumentsCapacity = 512;

    private readonly ILogger<FFRunner> _logger;
    private readonly IFFPaths _paths;

    /// <summary>
    /// Every live child, so shutdown can reach one whose cancellation token never fired. Each run
    /// already kills its own process, so this only covers the case where nothing cancels it.
    /// </summary>
    private readonly ConcurrentDictionary<int, Process> _running = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FFRunner"/> class.
    /// </summary>
    /// <param name="logger">The logger. Its enabled level also drives FFmpeg's own verbosity.</param>
    /// <param name="paths">Supplies the resolved ffmpeg and ffprobe locations.</param>
    public FFRunner(ILogger<FFRunner> logger, IFFPaths paths)
    {
        _logger = logger;
        _paths = paths;
    }

    /// <inheritdoc />
    public async Task<FFResult> RunAsync(FFRequest request, CancellationToken cancellationToken)
    {
        var session = await StartAsync(request, cancellationToken).ConfigureAwait(false);
        await using (session.ConfigureAwait(false))
        {
            return await session.Completion.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Build the global flags implied by the action and log level, then the operation's own
    /// arguments.
    /// </summary>
    private string BuildArguments(FFRequest request, in FFActionPolicy policy)
    {
        var sb = new StringBuilder(ArgumentsCapacity);

        sb.Append("-hide_banner -loglevel ")
            .Append(policy.StderrIsPayload ? FFLogLevel.ForPayload(_logger) : FFLogLevel.For(_logger))
            .Append(' ');

        if (policy.RequiresProgressStats)
        {
            // FFmpeg drops its status line below info. Asking for it explicitly keeps the log quiet
            // without the progress feed going dark.
            sb.Append("-stats ");
        }

        // FFprobe doesn't accept any of these arguments.
        if (!policy.ProbeOnly)
        {
            if (policy.Stdin == FFStdinMode.FireAndForget)
            {
                sb.Append("-nostdin ");
            }

            sb.Append(policy.Overwrite ? "-y " : "-n ");
        }

        request.BuildArguments(sb);

        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc />
    public async Task<IFFSession> StartAsync(FFRequest request, CancellationToken cancellationToken)
    {
        var started = await StartCoreAsync(request, cancellationToken).ConfigureAwait(false);

        return new FFSession(this, started, cancellationToken);
    }

    /// <inheritdoc />
    public string GetCommandLine(FFRequest request) => Resolve(request).CommandLine;

    /// <summary>
    /// Resolves a request's policy and determines the executable and the full argument string.
    /// <para>
    /// Both <see cref="GetCommandLine"/> and <see cref="StartCoreAsync"/> use this resolved command.
    /// </para>
    /// </summary>
    private ResolvedCommand Resolve(FFRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var policy = request.ResolvePolicy();

        // This is the only point that knows whether the request supplies
        // the progress signal the idle watchdog needs.
        policy.EnsureCoherent(request.Action, request.HasProgressSignal);

        return new ResolvedCommand(policy, ResolveBinaryPath(request, policy), BuildArguments(request, policy));
    }

    /// <summary>
    /// Guarantees what <see cref="FFActionPolicy.StderrIsPayload"/> promises: an action reading its
    /// own stderr as the answer gets a sink that keeps all of it.
    /// <para>
    /// The log-level floor alone is not enough. The default sink retains a trailing window, which is
    /// right for output that only explains a failure and wrong for output that <em>is</em> the
    /// result — the part that matters could be anywhere in it. Leaving this to each caller makes the
    /// guarantee depend on every call site remembering, and the failure is silent: a truncated
    /// answer parses as no answer.
    /// </para>
    /// </summary>
    private static FFRequest UpgradeSinkIfPayload(FFRequest request, in FFActionPolicy policy)
        => policy.StderrIsPayload && !request.Stderr.RetainsEverything
            ? request with { Stderr = FFOutputSink.Complete() }
            : request;

    /// <summary>
    /// Picks the executable for a request: the one it names, or the resolved prober or encoder.
    /// </summary>
    private string ResolveBinaryPath(FFRequest request, in FFActionPolicy policy)
    {
        var fileName = request.BinaryPathOverride.Length > 0
            ? request.BinaryPathOverride
            : (policy.ProbeOnly ? _paths.ProbePath : _paths.EncoderPath);

        if (string.IsNullOrEmpty(fileName))
        {
            throw new FfmpegException(
                $"{request.Action}: no {(policy.ProbeOnly ? "ffprobe" : "ffmpeg")} path has been resolved.");
        }

        return fileName;
    }

    /// <summary>
    /// Launches the process and starts both drains, returning before it exits.
    /// </summary>
    private async Task<StartedProcess> StartCoreAsync(
        FFRequest request,
        CancellationToken cancellationToken)
    {
        var (policy, fileName, arguments) = Resolve(request);
        _logger.LogDebug("{Action}: {FileName} {Arguments}", request.Action, fileName, arguments);

        request = UpgradeSinkIfPayload(request, policy);

        var process = CreateProcess(request, fileName, arguments);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            // Nothing else will ever see this one, so it has to be released here.
            process.Dispose();

            _logger.LogError(ex, "{Action}: failed to start {FileName}", request.Action, fileName);
            throw new FfmpegException($"{request.Action}: failed to start {fileName}", ex);
        }

        _running[process.Id] = process;
        ApplyPriority(process, request, policy.Priority);

        switch (policy.Stdin)
        {
            case FFStdinMode.FireAndForget:
                TryCloseStdin(process, request);
                break;

            case FFStdinMode.WriteThenClose:
                await WriteQueryKeyAsync(process, request).ConfigureAwait(false);
                break;

            default:
                // ControlChannel: left open for the caller to steer through IFFSession.
                break;
        }

        // Drains start before the exit wait because a full pipe would block the child, which then never exits.
        var stderrTask = DrainStderrAsync(process, request);
        var stdoutTask = request.Stdout(process.StandardOutput.BaseStream, cancellationToken);

        return new StartedProcess(request, policy, process, fileName, arguments, stopwatch, stderrTask, stdoutTask);
    }

    /// <summary>
    /// Waits under the deadlines, terminates whatever is left, and builds the outcome. The process
    /// is disposed here, so this runs exactly once per start.
    /// </summary>
    private async Task<FFResult> SuperviseAsync(StartedProcess started, CancellationToken cancellationToken)
    {
        var (request, policy, process, fileName, arguments, stopwatch, stderrTask, stdoutTask) = started;
        var stopReason = FFStopReason.Unknown;

        try
        {
            using (var hardTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Unbounded (-1 ms) is accepted and never fires; zero fires at once, so no policy should use that.
                hardTimeout.CancelAfter(policy.Timeout);

                try
                {
                    stopReason = await WaitAsync(process, request, policy, cancellationToken, hardTimeout.Token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // Also covers a throw out of WaitAsync: disposing a Process does not kill it.
                    await TerminateAsync(process, policy, request).ConfigureAwait(false);
                    _running.TryRemove(process.Id, out _);
                }
            }

            // Freeze it here so Elapsed measures the child's lifetime rather than also counting the
            // drains below, and so every later read reports the same value.
            stopwatch.Stop();

            var stderr = await stderrTask.ConfigureAwait(false);

            try
            {
                await stdoutTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The consumer aborted because the process is already gone.
            }

            var result = new FFResult(
                stopReason == FFStopReason.Exited ? process.ExitCode : -1,
                stderr,
                stopwatch.Elapsed,
                stopReason);

            if (stopReason == FFStopReason.Cancelled)
            {
                // The caller asked for this, so it is not an FFmpeg failure and must not read as one.
                _logger.LogDebug("{Action}: cancelled after {Elapsed}", request.Action, stopwatch.Elapsed);
            }
            else if (!result.Succeeded)
            {
                _logger.LogError(
                    "{Action}: exited {ExitCode} after {Elapsed} ({Reason}). Command: {FileName} {Arguments}. stderr: {Stderr}",
                    request.Action,
                    result.ExitCode,
                    stopwatch.Elapsed,
                    stopReason,
                    fileName,
                    arguments,
                    stderr);
            }

            return result;
        }
        finally
        {
            process.Dispose();
        }
    }

    /// <summary>
    /// Waits under the wall clock, and under the idle watchdog when the request supplies a real
    /// progress signal. CPU time is deliberately not used as liveness: a process blocked on a slow
    /// network read accrues none and would be killed while working correctly.
    /// </summary>
    private static async Task<FFStopReason> WaitAsync(
        Process process,
        FFRequest request,
        FFActionPolicy policy,
        CancellationToken callerToken,
        CancellationToken hardToken)
    {
        if (!request.HasProgressSignal)
        {
            try
            {
                await process.WaitForExitAsync(hardToken).ConfigureAwait(false);
                return FFStopReason.Exited;
            }
            catch (OperationCanceledException)
            {
                return callerToken.IsCancellationRequested ? FFStopReason.Cancelled : FFStopReason.TimedOut;
            }
        }

        var lastProbe = SafeProbe(request.ProgressProbe);

        while (true)
        {
            using var idle = CancellationTokenSource.CreateLinkedTokenSource(hardToken);
            idle.CancelAfter(policy.IdleTimeout);

            try
            {
                await process.WaitForExitAsync(idle.Token).ConfigureAwait(false);
                return FFStopReason.Exited;
            }
            catch (OperationCanceledException)
            {
                if (callerToken.IsCancellationRequested)
                {
                    return FFStopReason.Cancelled;
                }

                if (hardToken.IsCancellationRequested)
                {
                    return FFStopReason.TimedOut;
                }

                var probe = SafeProbe(request.ProgressProbe);
                if (probe <= lastProbe)
                {
                    return FFStopReason.Stalled;
                }

                lastProbe = probe;
            }
        }
    }

    private static Process CreateProcess(FFRequest request, string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false,

            // An inherited handle under a service manager can block the child on a read forever.
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,

            WorkingDirectory = request.WorkingDirectory
        };

        return new Process { StartInfo = startInfo, EnableRaisingEvents = true };
    }

    private void TryCloseStdin(Process process, FFRequest request)
    {
        try
        {
            process.StandardInput.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "{Action}: could not close stdin", request.Action);
        }
    }

    /// <summary>
    /// Asks the question that <see cref="FFStdinMode.WriteThenClose"/> exists for, then closes stdin
    /// so the child sees end of input.
    /// </summary>
    private async Task WriteQueryKeyAsync(Process process, FFRequest request)
    {
        try
        {
            await process.StandardInput.WriteAsync(RuntimeKeyProbeRequest.QueryKey).ConfigureAwait(false);
            await process.StandardInput.FlushAsync().ConfigureAwait(false);
            process.StandardInput.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "{Action}: could not write the query key", request.Action);
        }
    }

    /// <summary>
    /// Always drains stderr, whatever the sink does with it: a full pipe blocks the child, and a
    /// blocked child never exits.
    /// </summary>
    private async Task<string> DrainStderrAsync(Process process, FFRequest request)
    {
        try
        {
            var line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
            while (line is not null)
            {
                await request.Stderr.WriteLineAsync(line, CancellationToken.None).ConfigureAwait(false);
                line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "{Action}: stderr drain ended early", request.Action);
        }

        return request.Stderr.GetRetainedText();
    }

    /// <summary>
    /// Ends the process, asking first where the action allows it. Called from a <c>finally</c>, so it
    /// never throws: an exception here would replace whatever sent us down that path.
    /// </summary>
    private async Task TerminateAsync(Process process, FFActionPolicy policy, FFRequest request)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            // Asking is best-effort and must never cost us the kill. Only an exit skips it — a failed
            // request to stop falls through, because the caller is left with a live process either
            // way and this is the last chance to reap it. SuperviseAsync drops the process from
            // _running the moment this returns, so anything still alive here is beyond the reach of
            // the shutdown sweep too.
            if (policy.Stdin == FFStdinMode.ControlChannel
                && await TryStopPolitelyAsync(process, policy, request).ConfigureAwait(false))
            {
                return;
            }

            process.Kill(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Action}: failed to terminate the process", request.Action);
        }
    }

    /// <summary>
    /// Writes the quit key and waits out the action's grace period.
    /// </summary>
    /// <returns>
    /// Whether the process actually exited. Any failure reports <c>false</c> so the caller escalates.
    /// </returns>
    private async Task<bool> TryStopPolitelyAsync(Process process, FFActionPolicy policy, FFRequest request)
    {
        try
        {
            await process.StandardInput.WriteLineAsync("q").ConfigureAwait(false);
            await process.StandardInput.FlushAsync().ConfigureAwait(false);

            using var grace = new CancellationTokenSource(policy.GracefulStopTimeout);
            await process.WaitForExitAsync(grace.Token).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            // Covers the grace expiring and stdin not being writable at all. A child that has
            // closed its stdin makes the write throw an IOException on a broken pipe. A handle
            // already disposed throws ObjectDisposedException. Neither may cost us the kill: the
            // caller is left with a live process either way.
            _logger.LogDebug(ex, "{Action}: did not stop when asked; killing", request.Action);

            return false;
        }
    }

    private void ApplyPriority(Process process, FFRequest request, ProcessPriorityClass priority)
    {
        if (priority == FFDefaults.InheritPriority)
        {
            return;
        }

        try
        {
            process.PriorityClass = priority;
        }
        catch (Exception ex)
        {
            // A short-lived child can exit before this runs.
            _logger.LogDebug(ex, "{Action}: could not set process priority to {Priority}", request.Action, priority);
        }
    }

    /// <summary>
    /// Kills anything still running. The server is going down, so there is no point asking politely
    /// or waiting; a run being cancelled normally has already gone through <c>TerminateAsync</c>.
    /// </summary>
    public void Dispose()
    {
        foreach (var (_, process) in _running)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not kill FFmpeg process {ProcessId} during shutdown", process.Id);
            }
        }

        _running.Clear();
    }

    private static long SafeProbe(Func<long> probe)
    {
        try
        {
            return probe();
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// How a request will run, before anything has been launched. Produced only by
    /// <see cref="Resolve"/>.
    /// </summary>
    private readonly record struct ResolvedCommand(
        FFActionPolicy Policy,
        string FileName,
        string Arguments)
    {
        /// <summary>
        /// Gets the command line as it would be typed into a shell. The one place that decides how a
        /// command is rendered as text.
        /// </summary>
        public string CommandLine => FileName + " " + Arguments;
    }

    /// <summary>
    /// A launched process plus everything needed to supervise it. Deconstructed rather than passed
    /// as eight arguments.
    /// </summary>
    private sealed record StartedProcess(
        FFRequest Request,
        FFActionPolicy Policy,
        Process Process,
        string FileName,
        string Arguments,
        Stopwatch Stopwatch,
        Task<string> StderrTask,
        Task StdoutTask);

    /// <summary>
    /// Hands a running process back to the caller. Supervision starts immediately, so the process
    /// is still bounded by its deadlines even if the caller never touches the session again.
    /// </summary>
    private sealed class FFSession : IFFSession
    {
        private readonly StartedProcess _started;
        private readonly CancellationTokenSource _stopSignal;

        public FFSession(FFRunner runner, StartedProcess started, CancellationToken cancellationToken)
        {
            _started = started;
            _stopSignal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ProcessId = started.Process.Id;
            Completion = SuperviseThenReleaseAsync(runner, started);
        }

        public int ProcessId { get; }

        public Task<FFResult> Completion { get; }

        public async Task SendKeyAsync(string key, CancellationToken cancellationToken)
        {
            if (_started.Policy.Stdin != FFStdinMode.ControlChannel)
            {
                throw new InvalidOperationException($"{_started.Request.Action} is not steerable.");
            }

            await _started.Process.StandardInput.WriteAsync(key.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _started.Process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Releases the linked token source once supervision ends, so a caller that only awaits
        /// <see cref="Completion"/> — which is the normal shape — leaks nothing by never disposing.
        /// </summary>
        private async Task<FFResult> SuperviseThenReleaseAsync(FFRunner runner, StartedProcess started)
        {
            try
            {
                return await runner.SuperviseAsync(started, _stopSignal.Token).ConfigureAwait(false);
            }
            finally
            {
                _stopSignal.Dispose();
            }
        }

        public async Task<FFResult> StopAsync(CancellationToken cancellationToken)
        {
            // Cancelling is what drives the graceful stop: supervision writes q, waits the action's
            // grace period, then kills the tree.
            try
            {
                await _stopSignal.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Already finished and released itself; the result below is what mattered.
            }

            // Honour the caller's token: a stuck completion must not pin the caller indefinitely.
            return await Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            // The token source releases itself when supervision ends, so this only has to make sure
            // supervision has in fact ended. Disposing again is harmless and keeps the ownership
            // obvious to anyone reading the class.
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            _stopSignal.Dispose();
        }
    }
}
