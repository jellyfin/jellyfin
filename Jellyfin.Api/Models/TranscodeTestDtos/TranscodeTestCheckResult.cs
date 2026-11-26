namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The result of one compatibility check.
/// </summary>
public class TranscodeTestCheckResult
{
    /// <summary>
    /// Gets or sets what was tested.
    /// </summary>
    public TranscodeTestCheckKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the codec, for example hevc.
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bit depth of the test clip.
    /// </summary>
    public int BitDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test clip is HDR.
    /// </summary>
    public bool Hdr { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the matching setting is on, or <c>null</c> when there is none.
    /// </summary>
    public bool? EnabledInSettings { get; set; }

    /// <summary>
    /// Gets or sets the outcome.
    /// </summary>
    public TranscodeTestOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets what to change in the settings.
    /// </summary>
    public TranscodeTestAdvice Advice { get; set; }

    /// <summary>
    /// Gets or sets the ffmpeg encoder or filter that was used.
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the frames per second of the check, when it ran.
    /// </summary>
    public double? Fps { get; set; }

    /// <summary>
    /// Gets or sets the end of ffmpeg's error output, when it failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets how many frames a failed decoding check decoded, when ffmpeg said.
    /// </summary>
    public int? FramesDecoded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a failed decoding check decoded frames before it failed, so a later
    /// step of the pipeline failed rather than the decoder.
    /// </summary>
    public bool FailedAfterDecoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the outcome is <see cref="TranscodeTestOutcome.NotSupported"/> because
    /// ffmpeg got no frame through while the H.264 8-bit check of the same kind passed: the setup works, and the
    /// hardware lacks this format.
    /// </summary>
    public bool ReferencePassed { get; set; }
}
