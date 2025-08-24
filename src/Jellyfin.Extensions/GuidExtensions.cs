using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

    /// <summary>
    /// Convert a guid to an upper case string.
    /// </summary>
    /// <param name="guid">The guid.</param>
    /// <param name="format">The format to convert the guid to.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>The uppercase string.</returns>
    public static string ToUpper(this Guid guid, string format = "N", IFormatProvider? formatProvider = null)
    {
        Span<char> destination = stackalloc char[68];
        if (!guid.TryFormat(destination, out var charsWritten, format))
        {
            return guid.ToString(format, formatProvider).ToUpperInvariant();
        }

        destination = destination[..charsWritten];
        if (Ascii.ToUpperInPlace(destination, out _) != OperationStatus.Done)
        {
            return guid.ToString(format, formatProvider).ToUpperInvariant();
        }

        return destination.ToString();
    }
}
