using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Jellyfin.Extensions;

namespace Jellyfin.Server.Implementations;

/// <summary>
/// Makes the values that identify a row fit the key column they are stored in.
/// </summary>
/// <remarks>
/// <para>
/// A value that is not longer than its column is returned as it is. A longer one is replaced by a prefix of itself,
/// a dash and the SHA-256 hash of the whole value, written as lowercase hexadecimal, so that the result is at most as
/// long as the column and two long values that start alike stay different. The same value always gives the same
/// result, and normalizing a result again does not change it. The hash is taken over the UTF-8 bytes of the value, so
/// a lone surrogate in it is hashed as the replacement character U+FFFD.
/// </para>
/// <para>
/// Both the code that stores a value and the code that looks it up must normalize it, or they will not find each
/// other's rows.
/// </para>
/// </remarks>
public static class KeyNormalizer
{
    /// <summary>
    /// The maximum length of a user data key.
    /// </summary>
    public const int UserDataKeyMaxLength = KeyLengths.UserDataKey;

    /// <summary>
    /// The maximum length of a device id.
    /// </summary>
    public const int DeviceIdMaxLength = KeyLengths.DeviceId;

    private const int HashLength = 64;

    private const int SeparatorLength = 1;

    /// <summary>
    /// Makes a user data key fit its column.
    /// </summary>
    /// <param name="key">The user data key.</param>
    /// <returns>The key, or its normalized form when it is too long.</returns>
    public static string NormalizeUserDataKey(string key)
        => Normalize(key, UserDataKeyMaxLength);

    /// <summary>
    /// Makes a device id fit its column.
    /// </summary>
    /// <param name="deviceId">The device id.</param>
    /// <returns>The device id, or its normalized form when it is too long.</returns>
    public static string NormalizeDeviceId(string deviceId)
        => Normalize(deviceId, DeviceIdMaxLength);

    /// <summary>
    /// Makes the device ids of an enabled devices list match the ids the devices are stored under.
    /// </summary>
    /// <param name="deviceIds">The device ids.</param>
    /// <returns>The device ids without NUL characters, each one shortened when it is too long and without duplicates.</returns>
    public static string[] NormalizeDeviceIds(IEnumerable<string> deviceIds)
    {
        ArgumentNullException.ThrowIfNull(deviceIds);
        return deviceIds.Select(e => NormalizeDeviceId(e.RemoveNullCharacters())).Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Makes a value fit a column of the given length.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="maxLength">The maximum length of the column.</param>
    /// <returns>The value, or its normalized form when it is longer than <paramref name="maxLength"/>.</returns>
    internal static string Normalize(string value, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, HashLength + SeparatorLength);

        if (value.Length <= maxLength)
        {
            return value;
        }

        var prefixLength = maxLength - HashLength - SeparatorLength;
        if (prefixLength > 0 && char.IsHighSurrogate(value[prefixLength - 1]) && char.IsLowSurrogate(value[prefixLength]))
        {
            prefixLength--;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        return string.Concat(value.AsSpan(0, prefixLength), "-", hash);
    }
}
