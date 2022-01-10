using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Cryptography;
using static MediaBrowser.Model.Cryptography.Constants;

namespace Emby.Server.Implementations.Cryptography
{
    /// <summary>
    /// Class providing abstractions over cryptographic functions.
    /// </summary>
    public class CryptographyProvider : ICryptoProvider
    {
        // TODO: remove when not needed for backwards compat
        private static readonly HashSet<string> _supportedHashMethods = new HashSet<string>()
            {
                "MD5",
                "System.Security.Cryptography.MD5",
                "SHA",
                "SHA1",
                "System.Security.Cryptography.SHA1",
                "SHA256",
                "SHA-256",
                "System.Security.Cryptography.SHA256",
                "SHA384",
                "SHA-384",
                "System.Security.Cryptography.SHA384",
                "SHA512",
                "SHA-512",
                "System.Security.Cryptography.SHA512"
            };

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

            if (!_supportedHashMethods.Contains(hash.Id))
            {
                throw new CryptographicException($"Requested hash method is not supported: {hash.Id}");
            }

            using var h = HashAlgorithm.Create(hash.Id) ?? throw new ResourceNotFoundException($"Unknown hash method: {hash.Id}.");
            var bytes = Encoding.UTF8.GetBytes(password.ToArray());
            if (hash.Salt.Length == 0)
            {
                return hash.Hash.SequenceEqual(h.ComputeHash(bytes));
            }

            byte[] salted = new byte[bytes.Length + hash.Salt.Length];
            Array.Copy(bytes, salted, bytes.Length);
            hash.Salt.CopyTo(salted.AsSpan(bytes.Length));
            return hash.Hash.SequenceEqual(h.ComputeHash(salted));
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
