#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using MediaBrowser.Model.Cryptography;
using static MediaBrowser.Common.Cryptography.Constants;

namespace Emby.Server.Implementations.Cryptography
{
    /// <summary>
    /// Class providing abstractions over cryptographic functions.
    /// </summary>
    public class CryptographyProvider : ICryptoProvider, IDisposable
    {
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

        private RandomNumberGenerator _randomNumberGenerator;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptographyProvider"/> class.
        /// </summary>
        public CryptographyProvider()
        {
            // FIXME: When we get DotNet Standard 2.1 we need to revisit how we do the crypto
            // Currently supported hash methods from https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptoconfig?view=netcore-2.1
            // there might be a better way to autogenerate this list as dotnet updates, but I couldn't find one
            // Please note the default method of PBKDF2 is not included, it cannot be used to generate hashes cleanly as it is actually a pbkdf with sha1
            _randomNumberGenerator = RandomNumberGenerator.Create();
        }

        /// <inheritdoc />
        public string DefaultHashMethod => "PBKDF2";

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedHashMethods()
            => _supportedHashMethods;

        private byte[] PBKDF2(string method, byte[] bytes, byte[] salt, int iterations)
        {
            // downgrading for now as we need this library to be dotnetstandard compliant
            // with this downgrade we'll add a check to make sure we're on the downgrade method at the moment
            if (method != DefaultHashMethod)
            {
                throw new CryptographicException($"Cannot currently use PBKDF2 with requested hash method: {method}");
            }

            using var r = new Rfc2898DeriveBytes(bytes, salt, iterations);
            return r.GetBytes(32);
        }

        /// <inheritdoc />
        public byte[] ComputeHash(string hashMethod, byte[] bytes, byte[] salt)
        {
            if (hashMethod == DefaultHashMethod)
            {
                return PBKDF2(hashMethod, bytes, salt, DefaultIterations);
            }

            if (!_supportedHashMethods.Contains(hashMethod))
            {
                throw new CryptographicException($"Requested hash method is not supported: {hashMethod}");
            }

            using var h = HashAlgorithm.Create(hashMethod);
            if (salt.Length == 0)
            {
                return h.ComputeHash(bytes);
            }

            byte[] salted = new byte[bytes.Length + salt.Length];
            Array.Copy(bytes, salted, bytes.Length);
            Array.Copy(salt, 0, salted, bytes.Length, salt.Length);
            return h.ComputeHash(salted);
        }

        /// <inheritdoc />
        public byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt)
            => PBKDF2(DefaultHashMethod, bytes, salt, DefaultIterations);

        /// <inheritdoc />
        public byte[] GenerateSalt()
            => GenerateSalt(DefaultSaltLength);

        /// <inheritdoc />
        public byte[] GenerateSalt(int length)
        {
            byte[] salt = new byte[length];
            _randomNumberGenerator.GetBytes(salt);
            return salt;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _randomNumberGenerator.Dispose();
            }

            _disposed = true;
        }
    }
}
