#nullable enable

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// No-op <see cref="ISessionAudioFilterProvider"/> used when no plugin supplies session audio filters.
/// </summary>
public sealed class NoOpSessionAudioFilterProvider : ISessionAudioFilterProvider
{
    /// <inheritdoc />
    public string? GetAdditionalAudioFilter(string? playSessionId, string? deviceId, double startTimeSeconds = 0) => null;
}
