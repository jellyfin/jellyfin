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
    ControlChannel,

    /// <summary>
    /// Stdin is written once at startup and closed immediately after, so the process is not
    /// steerable thereafter. <c>-nostdin</c> is suppressed, since the write has to land.
    /// <para>
    /// Only the runtime-key probe, which asks its question by writing a key and then has nothing
    /// further to say. Distinguished from <see cref="ControlChannel"/> because a process whose stdin
    /// is gone cannot be asked to quit: terminating one has to go straight to the kill.
    /// </para>
    /// </summary>
    WriteThenClose
}
