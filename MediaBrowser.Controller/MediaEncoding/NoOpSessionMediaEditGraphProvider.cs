#nullable enable

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// No-op <see cref="ISessionMediaEditGraphProvider"/>.
/// </summary>
public sealed class NoOpSessionMediaEditGraphProvider : ISessionMediaEditGraphProvider
{
    /// <inheritdoc />
    public bool HasEditGraph(string? playSessionId, string? deviceId) => false;

    /// <inheritdoc />
    public SessionMediaEditGraph? GetEditGraph(string? playSessionId, string? deviceId, SessionMediaEditGraphContext context)
        => null;
}
