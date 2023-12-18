using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using MediaBrowser.Model.Cryptography;
using static MediaBrowser.Model.Cryptography.Constants;

namespace Emby.Server.Implementations.Cryptography
{
    /// <summary>
    /// Class providing abstractions over cryptographic functions.
    /// </summary>
    public class CryptographyProvider : ICryptoProvider
    {
        /// <inheritdoc />
        public string DefaultHashMethod => "PBKDF2-SHA512";

        /// <inheritdoc />
        public PasswordHash CreatePasswordHash(ReadOnlySpan<char> password)
        {
            byte[] salt = GenerateSalt();
            return new PasswordHash(
                DefaultHashMethod,
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    DefaultIterations,
                    HashAlgorithmName.SHA512,
                    DefaultOutputLength),
                salt,
                new Dictionary<string, string>
                {
                    { "iterations", DefaultIterations.ToString(CultureInfo.InvariantCulture) }
                });
        }

        /// <inheritdoc />
        public bool Verify(PasswordHash hash, ReadOnlySpan<char> password)
        {
            if (string.Equals(hash.Id, "PBKDF2", StringComparison.Ordinal))
            {
                return hash.Hash.SequenceEqual(
                    Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        hash.Salt,
                        int.Parse(hash.Parameters["iterations"], CultureInfo.InvariantCulture),
                        HashAlgorithmName.SHA1,
                        32));
            }

            if (string.Equals(hash.Id, "PBKDF2-SHA512", StringComparison.Ordinal))
            {
                return hash.Hash.SequenceEqual(
                    Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        hash.Salt,
                        int.Parse(hash.Parameters["iterations"], CultureInfo.InvariantCulture),
                        HashAlgorithmName.SHA512,
                        DefaultOutputLength));
            }

            throw new NotSupportedException($"Can't verify hash with id: {hash.Id}");
        }

        /// <inheritdoc />
        public byte[] GenerateSalt()
            => GenerateSalt(DefaultSaltLength);

        /// <inheritdoc />
        public byte[] GenerateSalt(int length)
        {
            var salt = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetNonZeroBytes(salt);
            return salt;
        }
    }
}
