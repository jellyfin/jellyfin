using System;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Extensions;

/// <summary>
/// Guid specific extensions.
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Determine whether the guid is default.
    /// </summary>
    /// <param name="guid">The guid.</param>
    /// <returns>Whether the guid is the default value.</returns>
    public static bool IsEmpty(this Guid guid)
        => guid.Equals(default);

    /// <summary>
    /// Determine whether the guid is null or default.
    /// </summary>
    /// <param name="guid">The guid.</param>
    /// <returns>Whether the guid is null or the default valueF.</returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this Guid? guid)
        => guid is null || guid.Value.IsEmpty();
}
