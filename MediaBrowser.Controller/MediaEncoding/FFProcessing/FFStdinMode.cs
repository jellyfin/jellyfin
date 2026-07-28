namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Whether an FFmpeg process is q-for-quittable. Sets both the <c>-nostdin</c> argument and the
/// disposition of the stdin handle. Derived from the action.
/// </summary>
public enum FFStdinMode
{
    /// <summary>
    /// Stdin is redirected and closed immediately, and <c>-nostdin</c> is emitted for the encoder.
    /// </summary>
    FireAndForget,

    /// <summary>
    /// Stdin stays open for FFmpeg's runtime keys. <c>-nostdin</c> is suppressed, since it would
    /// disable them.
    /// </summary>
    ControlChannel
}
