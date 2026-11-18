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
                var iterations = GetIterationsParameter(hash);
                return hash.Hash.SequenceEqual(
                    Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        hash.Salt,
                        iterations,
                        HashAlgorithmName.SHA1,
                        32));
            }

            if (string.Equals(hash.Id, "PBKDF2-SHA512", StringComparison.Ordinal))
            {
                var iterations = GetIterationsParameter(hash);
                return hash.Hash.SequenceEqual(
                    Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        hash.Salt,
                        iterations,
                        HashAlgorithmName.SHA512,
                        DefaultOutputLength));
            }

            throw new NotSupportedException($"Can't verify hash with id: {hash.Id}");
        }

        /// <summary>
        /// Extracts and validates the iterations parameter from a password hash.
        /// </summary>
        /// <param name="hash">The password hash containing parameters.</param>
        /// <returns>The number of iterations.</returns>
        /// <exception cref="FormatException">Thrown when iterations parameter is missing or invalid.</exception>
        private static int GetIterationsParameter(PasswordHash hash)
        {
            if (!hash.Parameters.TryGetValue("iterations", out var iterationsStr))
            {
                throw new FormatException($"Password hash with id '{hash.Id}' is missing required 'iterations' parameter.");
            }

            if (!int.TryParse(iterationsStr, CultureInfo.InvariantCulture, out var iterations))
            {
                throw new FormatException($"Password hash with id '{hash.Id}' has invalid 'iterations' parameter: '{iterationsStr}'.");
            }

            return iterations;
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
