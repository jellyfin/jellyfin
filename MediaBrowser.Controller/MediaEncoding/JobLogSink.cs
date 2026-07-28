using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Writes a delivery job's FFmpeg output to its log file and reports playback progress from the
/// same lines. FFmpeg's status line carries the position and bitrate, so the log and the progress
/// feed are one stream read once.
/// </summary>
public sealed class JobLogSink : FFOutputSink
{
    private readonly JobLogger _jobLogger;
    private readonly EncodingJobInfo _state;
    private readonly Stream _target;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobLogSink"/> class.
    /// </summary>
    /// <param name="logger">The logger backing progress reporting.</param>
    /// <param name="state">The job whose progress is updated.</param>
    /// <param name="target">The log file to write to. Owned by the caller.</param>
    public JobLogSink(ILogger logger, EncodingJobInfo state, Stream target)
    {
        _jobLogger = new JobLogger(logger);
        _state = state;
        _target = target;
    }

    /// <summary>
    /// Records one line of FFmpeg output.
    /// <para>
    /// No guard against the target being closed underneath this: the runner awaits the drain before
    /// completing the session, and the log stream is only disposed once that completes, so this
    /// cannot run concurrently with disposal. If the runner ever stops awaiting the drain first,
    /// that stops being true.
    /// </para>
    /// </summary>
    /// <param name="line">The line, without its terminator.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    public override async ValueTask WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        _jobLogger.ParseLogLine(line, _state);

        await _target.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine + line), cancellationToken).ConfigureAwait(false);

        // Flush per line so the log is readable while the transcode is still running.
        await _target.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string GetRetainedText() => string.Empty;
}
