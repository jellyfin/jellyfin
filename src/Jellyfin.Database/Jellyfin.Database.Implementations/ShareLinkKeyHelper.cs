using System;
using System.Security.Cryptography;

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Helpers for the per-user share link key stored on <see cref="Entities.User"/>.
    /// </summary>
    public static class ShareLinkKeyHelper
    {
        /// <summary>
        /// The length of the share link key in bytes.
        /// </summary>
        public const int KeyLengthBytes = 32;

        /// <summary>
        /// Generates a new random share link key encoded as base64 of <see cref="KeyLengthBytes"/> cryptographic random bytes.
        /// </summary>
        /// <returns>The newly generated share link key.</returns>
        public static string Generate()
        {
            byte[] bytes = new byte[KeyLengthBytes];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Returns a value indicating whether the share link key is <c>null</c> or blank.
        /// </summary>
        /// <param name="key">The share link key.</param>
        /// <returns><c>true</c> if the key is <c>null</c> or blank, otherwise <c>false</c>.</returns>
        public static bool IsNullOrBlank(string? key)
        {
            return string.IsNullOrWhiteSpace(key);
        }

        /// <summary>
        /// Tries to decode a share link key into its raw bytes.
        /// </summary>
        /// <param name="key">The base64 encoded share link key.</param>
        /// <param name="bytes">The decoded key bytes, or <c>null</c> if the key is invalid.</param>
        /// <returns><c>true</c> if the key decoded to exactly <see cref="KeyLengthBytes"/> bytes, otherwise <c>false</c>.</returns>
        public static bool TryDecode(string? key, out byte[]? bytes)
        {
            bytes = null;
            if (IsNullOrBlank(key))
            {
                return false;
            }

            try
            {
                byte[] decoded = Convert.FromBase64String(key!);
                if (decoded.Length != KeyLengthBytes)
                {
                    return false;
                }

                bytes = decoded;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
