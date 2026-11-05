using System;

namespace Jellyfin.Api.Helpers;

internal sealed record OfflineDownloadJobInfo(Guid Id, string Status, long Size, string Filename, string? ETag, DateTimeOffset ExpiresAt, string Quality, int? AudioStreamIndex, int? SubtitleStreamIndex, string SubtitleMode, long BytesProduced, string? Error);
