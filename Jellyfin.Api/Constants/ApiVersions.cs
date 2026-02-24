using System.Globalization;
using Asp.Versioning;

namespace Jellyfin.Api.Constants;

/// <summary>
/// API versions available for use in the current server binary.
/// As a general rule only N - 1 versions are supported and changes are only made in major releases.
/// Format is major version followed by release type and the date of publication.
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Version 10 published on 2026-02-24 as a stable release.
    /// </summary>
    public const string V1020260224 = "10-stable20260224";

    /// <summary>
    /// Version 12 published on 2026-02-24 as a stable release.
    /// </summary>
    public const string V1220260224 = "12-stable20260224";

    /// <summary>
    /// Returns a new instance of the <see cref="ApiVersion"/> class.
    /// </summary>
    /// <param name="version">A version string selected from the above constants.</param>
    /// <returns>An <see cref="ApiVersion"/> instance.</returns>
    public static ApiVersion Parse(string version)
    {
        var parts = version.Split("-", 2);

        return new ApiVersion(int.Parse(parts[0], CultureInfo.InvariantCulture), 0, parts[1]);
    }
}
