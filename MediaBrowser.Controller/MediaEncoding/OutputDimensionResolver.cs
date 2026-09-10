using System;
using System.Globalization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Resolves the display and storage dimensions a transcode operation will produce for a
/// given encoding state, before the transcode actually runs. Shares the SW scale branch
/// decision with <see cref="EncodingHelper.GetSwScaleFilter"/> via <see cref="ScaleBranching"/>
/// so the two can't disagree about which branch applies. Per-branch math mirrors the
/// scale-filter's AVExpression in C# with matching operation order.
/// Prefer <see cref="EncodingHelper.ResolveOutputDimensions"/>; this class is the math
/// layer it delegates to.
/// </summary>
internal static class OutputDimensionResolver
{
    /// <summary>
    /// Resolves output dims for <paramref name="state"/> and writes them onto it.
    /// Stream copy folds source SAR into source dims; HW chains call
    /// <see cref="EncodingHelper.GetFixedOutputSize"/>; SW chains run the C# mirror of
    /// <see cref="EncodingHelper.GetSwScaleFilter"/>. No-op if the source lacks video
    /// dimensions or the state is already resolved.
    /// </summary>
    /// <param name="state">The encoding state to mutate.</param>
    /// <param name="options">
    /// Encoding options. <see cref="EncodingOptions.HardwareAccelerationType"/> drives the HW/SW
    /// branch selection.
    /// </param>
    /// <param name="videoEncoder">
    /// The target video encoder name (e.g. <c>libx264</c>, <c>h264_vaapi</c>). Must match the
    /// encoder the transcoder will actually pick — passing a different value yields dims that
    /// won't match the actual encode. Obtain via <see cref="EncodingHelper.GetVideoEncoder"/>.
    /// </param>
    /// <param name="hasHardwareDecoder">
    /// Whether ffmpeg will use a HW decoder for this source. Obtain via
    /// <c>!string.IsNullOrEmpty(EncodingHelper.GetHardwareVideoDecoder(state, options))</c>.
    /// When false under a HW accel type whose chain has a SW-scale fallback, the chain takes
    /// that fallback internally and final dims come from SW math, not HW math.
    /// </param>
    internal static void Resolve(
        EncodingJobInfo state,
        EncodingOptions options,
        string? videoEncoder,
        bool hasHardwareDecoder)
    {
        if (state.ResolvedOutputWidth.HasValue && state.ResolvedOutputHeight.HasValue)
        {
            return;
        }

        if (state.VideoStream?.Width is not int storedWidth || state.VideoStream?.Height is not int storedHeight)
        {
            return;
        }

        // Run the same Width/Height → MaxWidth/MaxHeight conversion the real transcode pipeline
        // does in AttachMediaSourceInfo. Without this, the resolver dispatches to the fixed-dim
        // branch of GetSwScaleFilter that production never actually reaches for HTTP requests.
        EncodingHelper.EnforceResolutionLimit(state);

        var (sarNumerator, sarDenominator) = ParseSampleAspectRatio(state.VideoStream.SampleAspectRatio);

        if (EncodingHelper.IsCopyCodec(videoEncoder))
        {
            // Copy preserves source storage + rotation metadata. RESOLUTION reflects the
            // SPS-encoded display (storage × SAR); the player applies rotation at render time.
            // Round-to-nearest matches H.264 SPS-derived display width; values may be odd.
            state.ResolvedOutputWidth = (int)Math.Round((double)storedWidth * sarNumerator / sarDenominator);
            state.ResolvedOutputHeight = storedHeight;
            return;
        }

        // Transcode physically transposes pixels for ±90° rotation, stripping rotation metadata.
        // Both dims and SAR rotate with the frame: 720×576 SAR=16:11 → 576×720 SAR=11:16.
        var swapWidthAndHeight = Math.Abs((state.VideoStream.Rotation ?? 0) % 180) == 90;
        var sourceWidth = swapWidthAndHeight ? storedHeight : storedWidth;
        var sourceHeight = swapWidthAndHeight ? storedWidth : storedHeight;
        if (swapWidthAndHeight)
        {
            (sarNumerator, sarDenominator) = (sarDenominator, sarNumerator);
        }

        if (UsesHardwareScale(options, hasHardwareDecoder))
        {
            // HW filter chains bake literal output dims into the filter string via
            // GetFixedOutputSize (no scale filter expression to evaluate).
            var (hardwareWidth, hardwareHeight) = EncodingHelper.GetFixedOutputSize(
                sourceWidth,
                sourceHeight,
                state.BaseRequest.Width,
                state.BaseRequest.Height,
                state.BaseRequest.MaxWidth,
                state.BaseRequest.MaxHeight);
            if (hardwareWidth.HasValue && hardwareHeight.HasValue)
            {
                state.ResolvedOutputWidth = hardwareWidth;
                state.ResolvedOutputHeight = hardwareHeight;
            }

            return;
        }

        var (storageHeight, displayWidth) = ComputeSwScaleOutputDims(
            videoEncoder,
            sourceWidth,
            sourceHeight,
            sarNumerator,
            sarDenominator,
            state.MediaSource?.Video3DFormat,
            state.BaseRequest.Width,
            state.BaseRequest.Height,
            state.BaseRequest.MaxWidth,
            state.BaseRequest.MaxHeight);
        state.ResolvedOutputWidth = displayWidth;
        state.ResolvedOutputHeight = storageHeight;
    }

    // Mirrors EncodingHelper.GetVideoProcessingFilterChain's accel→chain dispatch plus each
    // HW chain's internal SW-scale fallback when no HW decoder is available (e.g.
    // GetVaapiVidFilterChain takes the GetSwScaleFilter branch when isSwDecoder).
    // VideoToolbox is HW-scale-only with no fallback. v4l2m2m has no HW filter chain
    // (ffmpeg ships no scale_v4l2m2m filter) — always SW.
    //
    // New HardwareAccelerationType values with HW filter chains must be added to this
    // switch — unhandled values default to SW (safe: wrong dim prediction is recoverable).
    private static bool UsesHardwareScale(EncodingOptions options, bool hasHardwareDecoder)
        => options.HardwareAccelerationType switch
        {
            HardwareAccelerationType.videotoolbox => true,
            HardwareAccelerationType.vaapi
                or HardwareAccelerationType.qsv
                or HardwareAccelerationType.nvenc
                or HardwareAccelerationType.amf
                or HardwareAccelerationType.rkmpp => hasHardwareDecoder,
            _ => false,
        };

    /// <summary>
    /// Parses an ffmpeg-style sample aspect ratio string (e.g. <c>16:11</c>) into numerator
    /// and denominator. Returns <c>(1, 1)</c> for null, empty, malformed, zero, or negative
    /// inputs — a defensive fallback that guards against a corrupt
    /// <see cref="MediaStream.SampleAspectRatio"/> dividing by zero downstream.
    /// </summary>
    /// <param name="sampleAspectRatio">SAR string of the form "N:D", or null.</param>
    /// <returns>(Numerator, Denominator). Both positive.</returns>
    internal static (int Numerator, int Denominator) ParseSampleAspectRatio(string? sampleAspectRatio)
    {
        if (!string.IsNullOrEmpty(sampleAspectRatio))
        {
            var parts = sampleAspectRatio.Split(':');
            if (parts.Length == 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator)
                && numerator > 0
                && denominator > 0)
            {
                return (numerator, denominator);
            }
        }

        return (1, 1);
    }

    // Mirrors each branch of EncodingHelper.GetSwScaleFilter in C# with matching float
    // operation order so results agree with ffmpeg's AVExpression evaluator. Switch uses
    // the same SwScaleBranch the filter-string builder picks, so a new branch added on
    // one side forces a compile error on the other until the enum is extended.
    private static (int StorageHeight, int DisplayWidth) ComputeSwScaleOutputDims(
        string? videoEncoder,
        int sourceWidth,
        int sourceHeight,
        int sourceSarNumerator,
        int sourceSarDenominator,
        Video3DFormat? threedFormat,
        int? requestedWidth,
        int? requestedHeight,
        int? requestedMaxWidth,
        int? requestedMaxHeight)
    {
        // ffmpeg's scale filter treats 0 as "use source dim" in an expression. Normalize
        // 0/null the same way so callers that send Width=0 to mean "unset" are handled
        // consistently with the filter's runtime behaviour.
        requestedWidth = requestedWidth > 0 ? requestedWidth : null;
        requestedHeight = requestedHeight > 0 ? requestedHeight : null;
        requestedMaxWidth = requestedMaxWidth > 0 ? requestedMaxWidth : null;
        requestedMaxHeight = requestedMaxHeight > 0 ? requestedMaxHeight : null;

        var decision = ScaleBranching.DecideSwScale(
            videoEncoder,
            threedFormat,
            requestedWidth,
            requestedHeight,
            requestedMaxWidth,
            requestedMaxHeight);

        // Match ffmpeg AVExpression evaluation order: separate divisions, then multiplication.
        var storageAspect = (double)sourceWidth / sourceHeight;
        var sampleAspectRatio = (double)sourceSarNumerator / sourceSarDenominator;
        var targetAspectRatio = decision.IsMjpeg ? storageAspect * sampleAspectRatio : storageAspect;
        var sourceDisplayAspectRatio = storageAspect * sampleAspectRatio;

        int storageHeight;
        double effectiveDisplayAspectRatio = sourceDisplayAspectRatio;

        // The branch decision in DecideSwScale guarantees specific fields are non-null per
        // branch. Extract them as non-nullable locals so the static analyzer can prove
        // they're never dereferenced as null.
        switch (decision.Branch)
        {
            case SwScaleBranch.FixedWHv4l2:
            {
                var fixedHeight = RequiredForBranch(requestedHeight, decision.Branch);
                storageHeight = (fixedHeight / 2) * 2;
                break;
            }

            case SwScaleBranch.FixedWH:
            {
                var fixedWidth = RequiredForBranch(requestedWidth, decision.Branch);
                var fixedHeight = RequiredForBranch(requestedHeight, decision.Branch);
                (storageHeight, effectiveDisplayAspectRatio) = ComputeFixedSwDims(
                    threedFormat,
                    fixedWidth,
                    fixedHeight,
                    sourceWidth,
                    sourceHeight,
                    sourceDisplayAspectRatio);
                break;
            }

            case SwScaleBranch.MaxWMaxH:
            {
                var maxWidth = RequiredForBranch(requestedMaxWidth, decision.Branch);
                var maxHeight = RequiredForBranch(requestedMaxHeight, decision.Branch);
                // scale=…:trunc(min(max(iw/targetAr,ih),min(MW/targetAr,MH))/2)*2
                var rawHeight = Math.Min(
                    Math.Max(sourceWidth / targetAspectRatio, sourceHeight),
                    Math.Min(maxWidth / targetAspectRatio, maxHeight));
                storageHeight = ((int)(rawHeight / 2)) * 2;
                break;
            }

            case SwScaleBranch.FixedW3D:
            {
                var fixedWidth = RequiredForBranch(requestedWidth, decision.Branch);
                (storageHeight, effectiveDisplayAspectRatio) = ComputeFixedSwDims(
                    threedFormat,
                    fixedWidth,
                    0,
                    sourceWidth,
                    sourceHeight,
                    sourceDisplayAspectRatio);
                break;
            }

            case SwScaleBranch.FixedW:
            {
                var fixedWidth = RequiredForBranch(requestedWidth, decision.Branch);
                // scale={W}:trunc(ow/targetAr/2)*2
                storageHeight = ((int)(fixedWidth / targetAspectRatio / 2)) * 2;
                break;
            }

            case SwScaleBranch.FixedH:
            {
                var fixedHeight = RequiredForBranch(requestedHeight, decision.Branch);
                // scale=…:{H}
                storageHeight = fixedHeight;
                break;
            }

            case SwScaleBranch.MaxW:
            {
                var maxWidth = RequiredForBranch(requestedMaxWidth, decision.Branch);
                // scale=trunc(min(max(iw,ih*targetAr),MW)/scaleVal)*scaleVal:trunc(ow/targetAr/2)*2
                var rawStorageWidth = Math.Min(Math.Max(sourceWidth, sourceHeight * targetAspectRatio), maxWidth);
                var alignedStorageWidth = ((int)(rawStorageWidth / decision.ScaleVal)) * decision.ScaleVal;
                storageHeight = ((int)(alignedStorageWidth / targetAspectRatio / 2)) * 2;
                break;
            }

            case SwScaleBranch.MaxH:
            {
                var maxHeight = RequiredForBranch(requestedMaxHeight, decision.Branch);
                // scale=…:min(max(iw/targetAr,ih),MH)
                var rawHeight = Math.Min(
                    Math.Max(sourceWidth / targetAspectRatio, sourceHeight),
                    (double)maxHeight);
                storageHeight = (int)rawHeight;
                break;
            }

            case SwScaleBranch.None:
                // No sizing requested — source dims unchanged.
                storageHeight = sourceHeight;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(videoEncoder),
                    decision.Branch,
                    "Unhandled SwScaleBranch value.");
        }

        // Round-to-nearest matches H.264 SPS-derived display width; values may be odd.
        var displayWidth = (int)Math.Round(effectiveDisplayAspectRatio * storageHeight);
        return (storageHeight, displayWidth);
    }

    // Asserts that a parameter required by the given SwScaleBranch was supplied. Throws if
    // the precondition was violated by ScaleBranching.DecideSwScale (a programming error).
    private static int RequiredForBranch(int? value, SwScaleBranch branch)
        => value ?? throw new InvalidOperationException(
            $"SwScaleBranch.{branch} requires a non-null requested dimension; ScaleBranching.DecideSwScale invariant violated.");

    private static (int StorageHeight, double EffectiveDisplayAspectRatio) ComputeFixedSwDims(
        Video3DFormat? threedFormat,
        int requestedWidth,
        int requestedHeight,
        int sourceWidth,
        int sourceHeight,
        double sourceDisplayAspectRatio)
    {
        if (threedFormat.HasValue)
        {
            // Per-variant effective DAR after the chain's setdar=dar=a step.
            var threeDDisplayAspectRatio = threedFormat.Value switch
            {
                Video3DFormat.HalfSideBySide => (double)sourceWidth / sourceHeight,
                Video3DFormat.FullSideBySide => (double)sourceWidth / (2.0 * sourceHeight),
                Video3DFormat.HalfTopAndBottom => 4.0 * sourceWidth / sourceHeight,
                Video3DFormat.FullTopAndBottom => 2.0 * sourceWidth / sourceHeight,
                _ => 0.0,
            };
            if (threeDDisplayAspectRatio > 0)
            {
                // Final scale={W}:trunc({W}/dar/2)*2
                var threeDStorageHeight = ((int)(requestedWidth / threeDDisplayAspectRatio / 2)) * 2;
                return (threeDStorageHeight, threeDDisplayAspectRatio);
            }
        }

        // Non-3D, or unmapped 3D (e.g. MVC) — falls to GetFixedSwScaleFilter's default block.
        if (requestedHeight > 0)
        {
            return ((requestedHeight / 2) * 2, sourceDisplayAspectRatio);
        }

        // Fixed W only, threeDFormat hit the default case. Filter hardcodes `a` (storage
        // ratio), not targetAr — matches GetFixedSwScaleFilter's default branch.
        var storageAspect = (double)sourceWidth / sourceHeight;
        var storageHeight = ((int)(requestedWidth / storageAspect / 2)) * 2;
        return (storageHeight, sourceDisplayAspectRatio);
    }
}
