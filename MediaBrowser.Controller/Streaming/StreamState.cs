using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// The stream state dto.
/// </summary>
public class StreamState : EncodingJobInfo, IDisposable
{
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ITranscodeManager _transcodeManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamState" /> class.
    /// </summary>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager" /> interface.</param>
    /// <param name="transcodingType">The <see cref="TranscodingJobType" />.</param>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager" /> singleton.</param>
    public StreamState(IMediaSourceManager mediaSourceManager, TranscodingJobType transcodingType, ITranscodeManager transcodeManager)
        : base(transcodingType)
    {
        _mediaSourceManager = mediaSourceManager;
        _transcodeManager = transcodeManager;
    }

    /// <summary>
    /// Gets or sets the requested url.
    /// </summary>
    public string? RequestedUrl { get; set; }

    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    public StreamingRequestDto Request
    {
        get => (StreamingRequestDto)BaseRequest;
        set
        {
            BaseRequest = value;
            IsVideoRequest = VideoRequest is not null;
        }
    }

    /// <summary>
    /// Gets the video request.
    /// </summary>
    public VideoRequestDto? VideoRequest => Request as VideoRequestDto;

    /// <summary>
    /// Gets or sets the direct stream provider.
    /// </summary>
    /// <remarks>
    /// Deprecated.
    /// </remarks>
    public IDirectStreamProvider? DirectStreamProvider { get; set; }

    /// <summary>
    /// Gets or sets the path to wait for.
    /// </summary>
    public string? WaitForPath { get; set; }

    /// <summary>
    /// Gets a value indicating whether the request outputs video.
    /// </summary>
    public bool IsOutputVideo => Request is VideoRequestDto;

    /// <summary>
    /// Gets the segment length.
    /// </summary>
    public int SegmentLength
    {
        get
        {
            int baseLength;
            if (Request.SegmentLength.HasValue)
            {
                baseLength = Request.SegmentLength.Value;
            }
            else if (EncodingHelper.IsCopyCodec(OutputVideoCodec))
            {
                var userAgent = UserAgent ?? string.Empty;

                if (userAgent.Contains("AppleTV", StringComparison.OrdinalIgnoreCase)
                    || userAgent.Contains("cfnetwork", StringComparison.OrdinalIgnoreCase)
                    || userAgent.Contains("ipad", StringComparison.OrdinalIgnoreCase)
                    || userAgent.Contains("iphone", StringComparison.OrdinalIgnoreCase)
                    || userAgent.Contains("ipod", StringComparison.OrdinalIgnoreCase))
                {
                    baseLength = 6;
                }
                else if (IsSegmentedLiveStream)
                {
                    baseLength = 3;
                }
                else
                {
                    baseLength = 6;
                }
            }
            else
            {
                baseLength = 3;
            }

            // Scale segment length with playback speed so the player has enough buffer.
            // At 2x speed, segments are consumed twice as fast, so we double the length.
            if (Request.PlaybackSpeed.HasValue && Request.PlaybackSpeed.Value > 1.0)
            {
                baseLength = (int)Math.Ceiling(baseLength * Request.PlaybackSpeed.Value);
            }

            return baseLength;
        }
    }

    /// <summary>
    /// Gets the minimum number of segments.
    /// </summary>
    public int MinSegments
    {
        get
        {
            if (Request.MinSegments.HasValue)
            {
                return Request.MinSegments.Value;
            }

            return SegmentLength >= 10 ? 2 : 3;
        }
    }

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to estimate the content length.
    /// </summary>
    public bool EstimateContentLength { get; set; }

    /// <summary>
    /// Gets or sets the transcode seek info.
    /// </summary>
    public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

    /// <summary>
    /// Gets or sets the transcoding job.
    /// </summary>
    public TranscodingJob? TranscodingJob { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
    {
        _transcodeManager.ReportTranscodingProgress(TranscodingJob!, this, transcodingPosition, framerate, percentComplete, bytesTranscoded, bitRate);
    }

    /// <summary>
    /// Disposes the stream state.
    /// </summary>
    /// <param name="disposing">Whether the object is currently being disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // REVIEW: Is this the right place for this?
            if (MediaSource.RequiresClosing
                && string.IsNullOrWhiteSpace(Request.LiveStreamId)
                && !string.IsNullOrWhiteSpace(MediaSource.LiveStreamId))
            {
                _mediaSourceManager.CloseLiveStream(MediaSource.LiveStreamId).GetAwaiter().GetResult();
            }
        }

        TranscodingJob = null;

        _disposed = true;
    }
}
