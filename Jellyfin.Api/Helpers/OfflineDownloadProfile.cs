namespace Jellyfin.Api.Helpers;

/// <summary>
/// Bounded H.264/AAC settings for a portable offline file.
/// </summary>
/// <param name="MaxWidth">Maximum picture width.</param>
/// <param name="MaxHeight">Maximum picture height.</param>
/// <param name="VideoBitRate">Video bitrate in bits per second.</param>
/// <param name="AudioBitRate">Audio bitrate in bits per second.</param>
public sealed record OfflineDownloadProfile(int MaxWidth, int MaxHeight, int VideoBitRate, int AudioBitRate);
