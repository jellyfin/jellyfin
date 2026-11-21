using System;
using MediaBrowser.Model.Cryptography;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Verifies passwords against a dummy hash so that a failed login takes the same time whether an authentication provider is available,
    /// the user doesn't exist, has no password set, or entered the wrong password.
    /// This reduces the risk of user enumeration based on response timings.
    /// </summary>
    public sealed class DummyPasswordVerifier
    {
        private readonly ICryptoProvider _cryptoProvider;
        private readonly Lazy<PasswordHash> _dummyHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="DummyPasswordVerifier"/> class.
        /// </summary>
        /// <param name="cryptoProvider">The cryptography provider.</param>
        public DummyPasswordVerifier(ICryptoProvider cryptoProvider)
        {
            _cryptoProvider = cryptoProvider;

            // NOTE: The hashing function shall be consistent with the one used for actual user passwords.
            _dummyHash = new Lazy<PasswordHash>(() => _cryptoProvider.CreatePasswordHash(Guid.NewGuid().ToString()));
        }

        /// <summary>
        /// Verifies the password against the dummy hash and discards the result.
        /// </summary>
        /// <param name="password">The password supplied by the client.</param>
        public void Verify(string password)
        {
            _cryptoProvider.Verify(_dummyHash.Value, password);
        }
    }
}
