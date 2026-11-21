namespace Jellyfin.Api.Helpers;

/// <summary>Internal request items supplied only by the authenticated resumable preparation handler.</summary>
public static class OfflineDownloadContext
{
    /// <summary>A unique offline-only encoder session, never accepted from query parameters.</summary>
    public const string SessionKey = "OfflineDownload.Session";

    /// <summary>A job-scoped cancellation token, separate from the initiating client's connection.</summary>
    public const string CancellationKey = "OfflineDownload.Cancellation";
}
