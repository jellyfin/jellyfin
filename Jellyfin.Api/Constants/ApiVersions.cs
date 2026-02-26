using System.Globalization;
using Asp.Versioning;

namespace Jellyfin.Api.Constants;

/// <summary>
/// API versions available for use in the current server binary.
/// As a general rule only N - 1 versions are supported and changes are only made in major releases.
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Version 12.0 of the Jellyfin API specification.
    /// </summary>
    public const string V1200 = "12.0";

    /// <summary>
    /// Returns a new instance of the <see cref="ApiVersion"/> class.
    /// </summary>
    /// <param name="version">A version string selected from the above constants.</param>
    /// <returns>An <see cref="ApiVersion"/> instance.</returns>
    public static ApiVersion Parse(string version)
    {
        var parts = version.Split(".", 2);

        return new ApiVersion(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
