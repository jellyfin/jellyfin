using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Records a live stream to a file, copying the streams rather than re-encoding them.
/// </summary>
public sealed record RecordRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.Record;

    /// <summary>Gets the source to record, already quoted by the caller.</summary>
    public required string Input { get; init; }

    /// <summary>Gets the file to write, already quoted and escaped by the caller.</summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets the FFmpeg version, which decides whether the read-rate catch-up limit is understood.
    /// </summary>
    public required Version EncoderVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether decode timestamps are ignored. Some broadcast sources emit
    /// ones that move backwards, which stalls muxing.
    /// </summary>
    public bool IgnoreDts { get; init; }

    /// <summary>
    /// Gets a value indicating whether the container index is ignored, for sources whose index is
    /// absent or wrong.
    /// </summary>
    public bool IgnoreIndex { get; init; }

    /// <summary>
    /// Gets a value indicating whether presentation timestamps are generated. Live sources often
    /// arrive with none, and a recording without them is unseekable.
    /// </summary>
    public bool GeneratePts { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is read at its native frame rate rather than as
    /// fast as it arrives, which is what makes a recording track wall-clock time.
    /// </summary>
    public bool ReadAtNativeFramerate { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is looped and reconnected on failure, for
    /// sources that end or drop out mid-recording.
    /// </summary>
    public bool RequiresLooping { get; init; }

    /// <summary>
    /// Gets how long FFmpeg may spend examining the source before deciding its stream layout.
    /// Live sources need longer than files, since the opening moments may carry no keyframe.
    /// </summary>
    public TimeSpan AnalyzeDuration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets a value indicating whether subtitle streams are copied. When false they are dropped,
    /// since a live source's subtitles are frequently unmuxable into the target container.
    /// </summary>
    public bool CopySubtitles { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Correct minor audio drift by stretching or squeezing rather than dropping samples.
        builder.Append("-async 1");

        if (IgnoreDts || IgnoreIndex || GeneratePts)
        {
            // -fflags tells the demuxer how much of the source's own timing metadata to believe.
            builder.Append(" -fflags ");

            if (IgnoreDts)
            {
                // ignore decode timestamps and let the decoder order frames itself
                builder.Append("+igndts");
            }

            if (IgnoreIndex)
            {
                // ignore the container index and find the streams by scanning instead
                builder.Append("+ignidx");
            }

            if (GeneratePts)
            {
                // synthesise presentation timestamps from frame order where the source carries none
                builder.Append("+genpts");
            }
        }

        if (ReadAtNativeFramerate)
        {
            // -re paces reading to the source's own frame rate instead of consuming it as fast as
            // it arrives, so an hour of broadcast takes an hour rather than filling the disk.
            builder.Append(" -re");

            // FFmpeg 8 tightened how far -re may run ahead, which stalls a remux. Restoring the
            // older allowance keeps recordings from stuttering.
            if (EncoderVersion >= new Version(8, 0))
            {
                builder.Append(" -readrate_catchup 100");
            }
        }

        if (RequiresLooping)
        {
            // -stream_loop -1 restarts the source indefinitely, and the reconnect options let a
            // dropped network source resume rather than ending the recording. The delay caps how
            // long to wait between attempts.
            builder.Append(" -stream_loop -1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2");
        }

        // How long to examine the source before deciding its stream layout. In microseconds.
        builder.Append(" -analyzeduration ")
            .Append(((long)AnalyzeDuration.TotalMicroseconds).ToString(CultureInfo.InvariantCulture));

        builder.Append(" -i ").Append(Input);

        // Copy both streams: a recording re-encodes nothing, it only changes container. The second
        // +genpts applies to the output, where live sources routinely arrive with no usable
        // presentation timestamps and a recording carrying none of its own is unseekable.
        builder.Append(" -codec:v:0 copy -fflags +genpts -codec:a:0 copy");

        // -sn drops subtitles entirely; a live source's are often unmuxable into the target.
        builder.Append(CopySubtitles ? " -codec:s copy" : " -sn");

        // Drop source metadata: it describes the live stream, not the recording.
        builder.Append(" -map_metadata -1 ").Append(OutputPath);
    }
}
