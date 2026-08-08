using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Decodes frames at a fixed interval and writes them as numbered stills for trickplay tiles.
/// </summary>
public sealed record TrickplayRequest : FFRequest
{
    /// <summary>The qscale used when the caller has no configured value.</summary>
    public const int DefaultQualityScale = 4;

    /// <summary>Best quality FFmpeg accepts for qscale.</summary>
    public const int BestQualityScale = 1;

    /// <summary>Worst quality FFmpeg accepts for qscale.</summary>
    public const int WorstQualityScale = 31;

    /// <inheritdoc />
    public override FFAction Action => FFAction.GenerateTrickplay;

    /// <summary>
    /// Gets the hardware-acceleration setup and input target as one string. Built by
    /// <c>EncodingHelper</c>, which owns the decoder selection and its thread clamps.
    /// </summary>
    public required string Input { get; init; }

    /// <summary>
    /// Gets the filter chain including the <c>-vf</c> flag, built by <c>EncodingHelper</c> which
    /// owns the hardware-dependent graph: which scaler, which tone-map, and which surface the
    /// frames live on all vary by decoder.
    /// </summary>
    public required string FilterChain { get; init; }

    /// <summary>
    /// Gets the source frame rate to rebuild timestamps at, or zero to leave them alone.
    /// <para>
    /// Some containers report timestamps that do not advance monotonically. Selecting frames by
    /// interval from those lands tiles at the wrong instants, so the presentation clock is
    /// recomputed from the frame number first. Keyframe-only extraction does not need this,
    /// because it takes whatever keyframes exist rather than sampling a timeline.
    /// </para>
    /// </summary>
    public double NormalizeTimestampsAtFrameRate { get; init; }

    /// <summary>Gets the numbered output pattern, such as <c>%08d.jpg</c>.</summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets the mjpeg encoder to use. It also decides how quality is expressed and whether a
    /// software fallback has to be offered.
    /// </summary>
    public required string VideoEncoder { get; init; }

    /// <summary>
    /// Gets the FFmpeg version, which decides how frame sync is spelled.
    /// </summary>
    public required Version EncoderVersion { get; init; }

    /// <summary>
    /// Gets the requested quality on FFmpeg's qscale scale, where <see cref="BestQualityScale"/> is
    /// best and <see cref="WorstQualityScale"/> is worst. Translated to whatever
    /// <see cref="VideoEncoder"/> actually understands.
    /// </summary>
    public int QualityScale { get; init; } = DefaultQualityScale;

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // -an -sn: tiles come from video only, so never decode audio or subtitles.
        builder.Append(Input).Append(" -an -sn ").Append(RenderFilterChain()).Append(' ');

        // No encoder thread count: mjpeg stills at tile resolution are trivial to encode, and the
        // encoder is fed by a decoder already pinned to one or two threads, so there is nothing to
        // parallelise. The decoder's count is set on the input side instead.
        builder.Append("-c:v ").Append(VideoEncoder).Append(' ');

        var (qualityFlag, qualityValue) = ResolveQuality();
        builder.Append(qualityFlag).Append(qualityValue.ToString(CultureInfo.InvariantCulture)).Append(' ');

        if (VideoEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase))
        {
            // let VideoToolbox fall back to its software encoder; some Intel Macs have no hardware mjpeg
            builder.Append("-allow_sw 1 ");
        }

        // pass source timestamps through unchanged so tile N maps to a known instant
        builder.Append(EncodingHelper.GetVideoSyncOption("0", EncoderVersion).Trim()).Append(' ');

        // image2 writes one file per frame, numbered by the pattern
        builder.Append("-f image2 \"").Append(OutputPath).Append('"');
    }

    /// <summary>
    /// Inserts the timestamp rebuild ahead of the interval selection, since <c>setpts</c> has to
    /// rewrite the clock before <c>fps</c> samples against it.
    /// </summary>
    private string RenderFilterChain()
    {
        if (NormalizeTimestampsAtFrameRate <= 0)
        {
            return FilterChain;
        }

        var fpsFilter = FilterChain.IndexOf("fps=", StringComparison.Ordinal);
        if (fpsFilter < 0)
        {
            throw new InvalidOperationException(
                "Timestamp normalisation was requested but the filter chain selects no frame rate: " + FilterChain);
        }

        return FilterChain.Insert(
            fpsFilter,
            string.Create(CultureInfo.InvariantCulture, $"setpts=N/{NormalizeTimestampsAtFrameRate:F3}/TB,"));
    }

    /// <summary>
    /// Translates <see cref="QualityScale"/> into the flag and value <see cref="VideoEncoder"/>
    /// understands. FFmpeg's qscale runs 1 (best) to 31 (worst); the hardware mjpeg encoders
    /// instead take a jpeg quality where higher is better, over differing ranges, so the scale has
    /// to be inverted per encoder rather than passed through.
    /// </summary>
    private (string Flag, int Value) ResolveQuality()
    {
        var quality = Math.Clamp(QualityScale, BestQualityScale, WorstQualityScale);

        if (VideoEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase)
            || VideoEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase))
        {
            // jpeg quality, 0-100
            return ("-global_quality:v ", 100 - ((quality - 1) * (100 / 30)));
        }

        if (VideoEncoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase))
        {
            // jpeg quality, but rkmpp caps at 99 rather than 100
            return ("-qp_init:v ", 99 - ((quality - 1) * (99 / 30)));
        }

        if (VideoEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase))
        {
            // jpeg quality scaled to QP2LAMBDA, still spelled as qscale
            return ("-qscale:v ", 118 - ((quality - 1) * (118 / 30)));
        }

        return ("-qscale:v ", quality);
    }
}
