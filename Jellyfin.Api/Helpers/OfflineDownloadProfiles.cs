using System;
using System.IO;
using System.Linq;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Fixed encoding presets for on-demand offline downloads.
/// </summary>
public static class OfflineDownloadProfiles
{
    /// <summary>
    /// Gets a whitelisted preset. Arbitrary encoder parameters are not accepted.
    /// </summary>
    /// <param name="name">The preset name.</param>
    /// <returns>The preset, or null for an unsupported name.</returns>
    public static OfflineDownloadProfile? Get(string? name)
        => name switch
        {
            "540p" => new OfflineDownloadProfile(960, 540, 600_000, 96_000),
            "720p" => new OfflineDownloadProfile(1280, 720, 1_000_000, 96_000),
            "1080p" => new OfflineDownloadProfile(1920, 1080, 2_000_000, 128_000),
            _ => null
        };

    /// <summary>
    /// Creates a portable attachment filename without source directory information.
    /// </summary>
    /// <param name="sourcePath">The original media path.</param>
    /// <param name="profile">A whitelisted preset name.</param>
    /// <returns>A safe MP4 filename.</returns>
    public static string CreateFileName(string sourcePath, string profile)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        if (Get(profile) is null)
        {
            throw new ArgumentException("Unsupported offline download profile.", nameof(profile));
        }

        var basename = sourcePath.Replace('\\', '/').Split('/').Last();
        var stem = Path.GetFileNameWithoutExtension(basename);
        const string forbidden = "<>:\"/\\|?*";
        stem = string.Concat(stem.Select(c => char.IsControl(c) || forbidden.Contains(c, StringComparison.Ordinal) ? '_' : c));
        stem = stem.Length > 120 ? stem[..120] : stem;
        stem = stem.Trim(' ', '.');
        if (string.IsNullOrEmpty(stem))
        {
            stem = "video";
        }

        return $"{stem} - {profile}.mp4";
    }
}
