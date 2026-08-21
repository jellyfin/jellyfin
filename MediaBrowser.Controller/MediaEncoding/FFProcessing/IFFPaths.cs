namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Holds the resolved locations of the FFmpeg and FFprobe binaries.
/// <para>
/// This exists as its own service so that the process runner does not have to depend on the media
/// encoder, which would be a cycle: the encoder is what runs the operations, and the runner is what
/// the encoder runs them through.
/// </para>
/// </summary>
public interface IFFPaths
{
    /// <summary>
    /// Gets the FFmpeg binary path, or empty when none has been resolved.
    /// </summary>
    string EncoderPath { get; }

    /// <summary>
    /// Gets the FFprobe binary path, derived from <see cref="EncoderPath"/>.
    /// </summary>
    string ProbePath { get; }

    /// <summary>
    /// Records the resolved encoder location and derives the prober's from it. FFprobe is never
    /// configured separately.
    /// </summary>
    /// <param name="encoderPath">The FFmpeg binary path, or empty to clear both.</param>
    void SetEncoderPath(string encoderPath);
}
