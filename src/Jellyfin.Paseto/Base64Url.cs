using System;
using System.Text;

namespace Jellyfin.Paseto;

/// <summary>
/// RFC 4648 base64url encoding without padding, with strict decoding that rejects
/// '=' padding characters and non-zero trailing bits.
/// </summary>
internal static class Base64Url
{
    /// <summary>
    /// Encodes a byte span as RFC 4648 base64url without padding.
    /// </summary>
    /// <param name="data">The bytes to encode.</param>
    /// <returns>The unpadded base64url representation of <paramref name="data"/>.</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        string encoded = Convert.ToBase64String(data).TrimEnd('=');
        return encoded.Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Strictly decodes an unpadded RFC 4648 base64url string.
    /// </summary>
    /// <param name="input">The unpadded base64url string to decode. '=' padding and non-zero trailing bits are rejected.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="FormatException">The input is not valid unpadded base64url with zero trailing bits.</exception>
    public static byte[] Decode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Array.Empty<byte>();
        }

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            bool isAlphabet = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
            if (!isAlphabet)
            {
                throw new FormatException("Base64url input contains invalid characters or padding.");
            }
        }

        int remainder = input.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Base64url input has an invalid length.");
        }

        if (remainder == 2 && (ValueOf(input[input.Length - 1]) & 0x0F) != 0)
        {
            throw new FormatException("Base64url input has non-zero trailing bits.");
        }

        if (remainder == 3 && (ValueOf(input[input.Length - 1]) & 0x03) != 0)
        {
            throw new FormatException("Base64url input has non-zero trailing bits.");
        }

        int padding = remainder == 0 ? 0 : 4 - remainder;
        string standard = input.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(standard + new string('=', padding));
    }

    /// <summary>
    /// Maps a base64url alphabet character to its 6-bit value.
    /// </summary>
    /// <param name="c">The alphabet character.</param>
    /// <returns>The 6-bit value of the character.</returns>
    private static int ValueOf(char c)
    {
        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A';
        }

        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 26;
        }

        if (c >= '0' && c <= '9')
        {
            return c - '0' + 52;
        }

        return c == '-' ? 62 : 63;
    }
}
