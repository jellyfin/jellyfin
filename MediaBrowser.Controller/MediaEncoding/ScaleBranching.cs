using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Shared SW-scale branch decision used by <see cref="EncodingHelper.GetSwScaleFilter"/> and
/// the output-dims resolver so the two can't disagree about which branch applies.
/// </summary>
public static class ScaleBranching
{
    private const string V4l2EncoderName = "h264_v4l2m2m";
    private const string MjpegSubstring = "mjpeg";

    /// <summary>
    /// Decides the SW scale branch plus encoder traits for a single request. Callers switch
    /// on <see cref="SwScaleDecision.Branch"/> and read <see cref="SwScaleDecision.ScaleVal"/>
    /// / <see cref="SwScaleDecision.TargetArExpression"/> directly — no separate calls to
    /// encoder-trait helpers needed.
    /// </summary>
    /// <param name="videoEncoder">Picked video encoder name.</param>
    /// <param name="threedFormat">Source 3D format, if any.</param>
    /// <param name="requestedWidth">Client-requested fixed width.</param>
    /// <param name="requestedHeight">Client-requested fixed height.</param>
    /// <param name="requestedMaxWidth">Client-requested max width.</param>
    /// <param name="requestedMaxHeight">Client-requested max height.</param>
    /// <returns>The decision record.</returns>
    public static SwScaleDecision DecideSwScale(
        string? videoEncoder,
        Video3DFormat? threedFormat,
        int? requestedWidth,
        int? requestedHeight,
        int? requestedMaxWidth,
        int? requestedMaxHeight)
    {
        var isV4l2 = IsV4l2Encoder(videoEncoder);
        var isMjpeg = IsMjpegEncoder(videoEncoder);
        var branch = PickSwScaleBranch(
            isV4l2,
            threedFormat,
            requestedWidth,
            requestedHeight,
            requestedMaxWidth,
            requestedMaxHeight);
        return new SwScaleDecision(
            branch,
            isV4l2 ? 64 : 2,
            isMjpeg ? "(a*sar)" : "a",
            isMjpeg);
    }

    private static bool IsV4l2Encoder(string? videoEncoder)
        => string.Equals(videoEncoder, V4l2EncoderName, StringComparison.OrdinalIgnoreCase);

    private static bool IsMjpegEncoder(string? videoEncoder)
        => videoEncoder is not null && videoEncoder.Contains(MjpegSubstring, StringComparison.OrdinalIgnoreCase);

    private static SwScaleBranch PickSwScaleBranch(
        bool isV4l2,
        Video3DFormat? threedFormat,
        int? requestedWidth,
        int? requestedHeight,
        int? requestedMaxWidth,
        int? requestedMaxHeight)
    {
        if (requestedWidth.HasValue && requestedHeight.HasValue)
        {
            return isV4l2 ? SwScaleBranch.FixedWHv4l2 : SwScaleBranch.FixedWH;
        }

        if (requestedMaxWidth.HasValue && requestedMaxHeight.HasValue)
        {
            return SwScaleBranch.MaxWMaxH;
        }

        if (requestedWidth.HasValue)
        {
            return threedFormat.HasValue ? SwScaleBranch.FixedW3D : SwScaleBranch.FixedW;
        }

        if (requestedHeight.HasValue)
        {
            return SwScaleBranch.FixedH;
        }

        if (requestedMaxWidth.HasValue)
        {
            return SwScaleBranch.MaxW;
        }

        if (requestedMaxHeight.HasValue)
        {
            return SwScaleBranch.MaxH;
        }

        return SwScaleBranch.None;
    }
}
