namespace MediaBrowser.Common.Net;

/// <summary>
/// Registered http client names.
/// </summary>
public static class NamedClient
{
    /// <summary>
    /// Gets the value for the default named http client which implements happy eyeballs.
    /// </summary>
    public const string Default = nameof(Default);

    /// <summary>
    /// Non happy eyeballs implementation.
    /// </summary>
    public const string DirectIp = nameof(DirectIp);
}
