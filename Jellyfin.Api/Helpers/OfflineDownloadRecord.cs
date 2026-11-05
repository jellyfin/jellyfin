using System;

namespace Jellyfin.Api.Helpers;

internal sealed class OfflineDownloadRecord
{
    public Guid Id { get; set; }

    public Guid Owner { get; set; }

    public Guid Item { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public string Filename { get; set; } = string.Empty;

    public string Quality { get; set; } = "720p";

    public int? AudioStreamIndex { get; set; }

    public int? SubtitleStreamIndex { get; set; }

    public string SubtitleMode { get; set; } = "none";

    public string Status { get; set; } = "Queued";

    public long Size { get; set; }

    public string? ETag { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
