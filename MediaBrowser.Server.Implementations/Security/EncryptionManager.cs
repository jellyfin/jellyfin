using MediaBrowser.Controller.Security;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Server.Implementations.Security
{
    public class EncryptionManager : IEncryptionManager
    {
        /// <summary>
        /// Encrypts the string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public string EncryptString(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            return Encoding.Default.GetString(ProtectedData.Protect(Encoding.Default.GetBytes(value), null, DataProtectionScope.LocalMachine));
        }

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public string DecryptString(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            return Encoding.Default.GetString(ProtectedData.Unprotect(Encoding.Default.GetBytes(value), null, DataProtectionScope.LocalMachine));
        }
    }
}
