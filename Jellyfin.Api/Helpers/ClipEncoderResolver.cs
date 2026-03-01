using MediaBrowser.Controller.MediaEncoding;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Resolves FFmpeg video/audio encoders and output container based on the requested codec.
/// H.264 uses libx264 + aac in mp4. H.265 uses libx265 + aac in mp4.
/// AV1 uses libsvtav1 (or libaom-av1 as fallback) + aac in mp4.
/// </summary>
internal static class ClipEncoderResolver
{
    /// <summary>
    /// Resolves the video encoder, audio encoder and container for a clip.
    /// </summary>
    /// <param name="mediaEncoder">The media encoder used to probe encoder availability.</param>
    /// <param name="requestedCodec">The codec requested by the client ("h264", "h265", "av1").</param>
    /// <returns>A tuple of (videoEncoder, container, audioEncoder).</returns>
    internal static (string VideoEncoder, string Container, string AudioEncoder) ResolveEncoders(
        IMediaEncoder mediaEncoder,
        string requestedCodec)
    {
        var audioEnc = ResolveAacEncoder(mediaEncoder);

        return requestedCodec.ToLowerInvariant() switch
        {
            "h265" or "hevc" => ("libx265", "mp4", audioEnc),
            "av1" => (mediaEncoder.SupportsEncoder("libsvtav1") ? "libsvtav1" : "libaom-av1", "mp4", audioEnc),
            _ => ("libx264", "mp4", audioEnc)
        };
    }

    /// <summary>Selects the best available AAC encoder.</summary>
    /// <param name="mediaEncoder">The media encoder used to probe encoder availability.</param>
    /// <returns>The name of the best available AAC encoder.</returns>
    internal static string ResolveAacEncoder(IMediaEncoder mediaEncoder)
    {
        if (mediaEncoder.SupportsEncoder("aac_at"))
        {
            return "aac_at";
        }

        if (mediaEncoder.SupportsEncoder("libfdk_aac"))
        {
            return "libfdk_aac";
        }

        return "aac";
    }
}
