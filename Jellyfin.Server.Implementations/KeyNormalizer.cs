using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Jellyfin.Extensions;

namespace Jellyfin.Server.Implementations;

/// <summary>
/// Makes the values that are part of a key fit the column they are stored in.
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
/// <para>
/// The text that metadata describes an item with, such as the role of a credit or the value of a tag, is cut to the
/// column instead. Cutting never splits a surrogate pair, and values that are equal up to the cut are one value.
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

    /// <summary>
    /// The maximum length of the role of a credit.
    /// </summary>
    public const int PersonRoleMaxLength = KeyLengths.PersonRole;

    /// <summary>
    /// The maximum length of the value of an item value, such as a tag or a genre.
    /// </summary>
    public const int ItemValueMaxLength = KeyLengths.ItemValue;

    /// <summary>
    /// The maximum length of the name of a metadata provider of an item.
    /// </summary>
    public const int ProviderIdMaxLength = KeyLengths.ProviderId;

    /// <summary>
    /// The maximum length of the key of a custom display preference.
    /// </summary>
    public const int DisplayPreferencesKeyMaxLength = KeyLengths.DisplayPreferencesKey;

    /// <summary>
    /// The maximum length of the name of a user.
    /// </summary>
    public const int UsernameMaxLength = KeyLengths.Username;

    /// <summary>
    /// The maximum length of the client name of display preferences.
    /// </summary>
    public const int DisplayPreferencesClientMaxLength = KeyLengths.DisplayPreferencesClient;

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
    /// Cuts the role of a credit to its column.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The role, or its beginning without trailing white space when it is too long.</returns>
    public static string CutPersonRole(string role)
        => Cut(role, PersonRoleMaxLength).TrimEnd();

    /// <summary>
    /// Cuts the value of an item value to its column.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, or its beginning when it is too long.</returns>
    public static string CutItemValue(string value)
        => Cut(value, ItemValueMaxLength);

    /// <summary>
    /// Cuts a value to at most the given number of characters without splitting a surrogate pair.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="maxLength">The maximum number of UTF-16 code units to keep.</param>
    /// <returns>The value itself when it fits, otherwise its beginning, one character shorter when the cut would
    /// fall between the two halves of a surrogate pair.</returns>
    internal static string Cut(string value, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length <= maxLength)
        {
            return value;
        }

        var length = maxLength;
        if (length > 0 && char.IsHighSurrogate(value[length - 1]) && char.IsLowSurrogate(value[length]))
        {
            length--;
        }

        return value[..length];
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
