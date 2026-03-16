using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.ClipDtos;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Runs an FFmpeg clip extraction process and updates a <see cref="ClipJob"/> with progress.
/// </summary>
internal sealed class ClipFfmpegRunner
{
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipFfmpegRunner"/> class.
    /// </summary>
    /// <param name="mediaEncoder">Used to resolve the FFmpeg binary path.</param>
    /// <param name="logger">Logger for info/error output.</param>
    public ClipFfmpegRunner(IMediaEncoder mediaEncoder, ILogger logger)
    {
        _mediaEncoder = mediaEncoder;
        _logger = logger;
    }

    /// <summary>
    /// Runs FFmpeg as a background process, parses stderr for progress, and updates the ClipJob.
    /// Releases the encoding semaphore when done (success, error, or cancellation).
    /// </summary>
    /// <param name="clipJob">The job to update with progress and completion state.</param>
    /// <param name="ffmpegArgs">The FFmpeg argument string.</param>
    /// <param name="jobs">The shared job dictionary, used for cleanup scheduling.</param>
    /// <param name="semaphore">The encoding semaphore to release when the job finishes.</param>
    /// <param name="cancellationToken">Token that triggers cancellation of the FFmpeg process.</param>
    /// <returns>A <see cref="Task"/> representing the background work.</returns>
    public async Task RunAsync(
        ClipJob clipJob,
        string ffmpegArgs,
        ConcurrentDictionary<string, ClipJob> jobs,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var ffmpegPath = _mediaEncoder.EncoderPath;
        var durationSeconds = clipJob.DurationTicks / 10_000_000.0;

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = ffmpegArgs,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();

            var fullStderr = await ReadStderrAsync(process, clipJob, durationSeconds, cancellationToken).ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            HandleExitCode(process.ExitCode, clipJob, fullStderr, jobs);
        }
        catch (OperationCanceledException)
        {
            clipJob.HasError = true;
            clipJob.ErrorMessage = "Clip extraction was cancelled.";
            await KillProcessIfRunningAsync(process).ConfigureAwait(false);
            ScheduleErrorJobCleanup(clipJob.ClipId, jobs);
        }
        catch (Exception ex)
        {
            clipJob.HasError = true;
            clipJob.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Clip extraction error: {ClipId}", clipJob.ClipId);
            ScheduleErrorJobCleanup(clipJob.ClipId, jobs);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<string> ReadStderrAsync(
        Process process,
        ClipJob clipJob,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        var stderr = process.StandardError;
        var buffer = new char[4096];
        var line = new StringBuilder();
        var fullStderr = new StringBuilder();

        while (!process.HasExited)
        {
            var read = await stderr.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                var ch = buffer[i];
                fullStderr.Append(ch);
                if (ch == '\r' || ch == '\n')
                {
                    ParseLine(line.ToString(), clipJob, durationSeconds);
                    line.Clear();
                }
                else
                {
                    line.Append(ch);
                }
            }
        }

        if (line.Length > 0)
        {
            ParseLine(line.ToString(), clipJob, durationSeconds);
        }

        return fullStderr.ToString();
    }

    private void HandleExitCode(
        int exitCode,
        ClipJob clipJob,
        string fullStderr,
        ConcurrentDictionary<string, ClipJob> jobs)
    {
        if (exitCode == 0)
        {
            clipJob.ProgressPercent = 100;
            clipJob.IsComplete = true;
            _logger.LogInformation("Clip extraction complete: {ClipId} -> {OutputPath}", clipJob.ClipId, clipJob.OutputPath);
            ScheduleCompletedJobCleanup(clipJob.ClipId, clipJob.OutputPath, jobs);
        }
        else
        {
            clipJob.HasError = true;
            clipJob.ErrorMessage = $"FFmpeg exited with code {exitCode}";
            _logger.LogError(
                "Clip extraction failed: {ClipId}, exit code {ExitCode}\nFFmpeg stderr:\n{Stderr}",
                clipJob.ClipId,
                exitCode,
                fullStderr);
            ScheduleErrorJobCleanup(clipJob.ClipId, jobs);
        }
    }

    private static async Task KillProcessIfRunningAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill();
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Parses a single FFmpeg stderr line to extract elapsed time and compute progress.
    /// FFmpeg outputs lines like: "frame= 123 fps=45 ... time=00:00:05.23 ..."
    /// With -ss before -i, timestamps reset to 0 at the start point.
    /// </summary>
    /// <param name="line">A single line from FFmpeg stderr output.</param>
    /// <param name="clipJob">The clip job whose progress will be updated.</param>
    /// <param name="durationSeconds">Total clip duration in seconds used to compute percent.</param>
    internal static void ParseLine(string line, ClipJob clipJob, double durationSeconds)
    {
        var timeIdx = line.IndexOf("time=", StringComparison.Ordinal);
        if (timeIdx < 0)
        {
            return;
        }

        var start = timeIdx + 5;
        if (start + 11 > line.Length)
        {
            return;
        }

        var timeStr = line.AsSpan(start, 11); // "HH:MM:SS.ff" = 11 chars
        if (TimeSpan.TryParseExact(timeStr, @"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture, out var elapsed)
            && durationSeconds > 0)
        {
            clipJob.ProgressPercent = Math.Min(99.9, elapsed.TotalSeconds / durationSeconds * 100.0);
        }
    }

    /// <summary>
    /// Schedules removal of a completed clip job and its output file after 48 hours.
    /// </summary>
    private static void ScheduleCompletedJobCleanup(string clipId, string outputPath, ConcurrentDictionary<string, ClipJob> jobs)
    {
        _ = Task.Delay(TimeSpan.FromHours(48), CancellationToken.None)
            .ContinueWith(
                _ =>
                {
                    if (jobs.TryRemove(clipId, out var job))
                    {
                        job.CancellationTokenSource.Dispose();
                        File.Delete(outputPath);
                    }
                },
                TaskScheduler.Default);
    }

    /// <summary>
    /// Schedules removal of a failed clip job from the dictionary after 1 hour.
    /// The SSE endpoint needs the job to remain briefly so the client can read the error.
    /// </summary>
    private static void ScheduleErrorJobCleanup(string clipId, ConcurrentDictionary<string, ClipJob> jobs)
    {
        _ = Task.Delay(TimeSpan.FromHours(1), CancellationToken.None)
            .ContinueWith(
                _ =>
                {
                    if (jobs.TryRemove(clipId, out var job))
                    {
                        job.CancellationTokenSource.Dispose();
                        TryDeleteFile(job.OutputPath);
                    }
                },
                TaskScheduler.Default);
    }

    private static void TryDeleteFile(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            File.Delete(path);
        }
    }
}
