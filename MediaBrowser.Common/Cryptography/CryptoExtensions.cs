using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MediaBrowser.Model.Cryptography;
using static MediaBrowser.Common.Cryptography.Constants;

namespace MediaBrowser.Common.Cryptography
{
    /// <summary>
    /// Class containing extension methods for working with Jellyfin cryptography objects.
    /// </summary>
    public static class CryptoExtensions
    {
        /// <summary>
        /// Creates a new <see cref="PasswordHash" /> instance.
        /// </summary>
        /// <param name="cryptoProvider">The <see cref="ICryptoProvider" /> instance used.</param>
        /// <param name="password">The password that will be hashed.</param>
        /// <returns>A <see cref="PasswordHash" /> instance with the hash method, hash, salt and number of iterations.</returns>
        public static PasswordHash CreatePasswordHash(this ICryptoProvider cryptoProvider, string password)
        {
            byte[] salt = cryptoProvider.GenerateSalt();
            return new PasswordHash(
                cryptoProvider.DefaultHashMethod,
                cryptoProvider.ComputeHashWithDefaultMethod(
                    Encoding.UTF8.GetBytes(password),
                    salt),
                salt,
                new Dictionary<string, string>
                {
                    { "iterations", DefaultIterations.ToString(CultureInfo.InvariantCulture) }
                });
        }
    }
}
