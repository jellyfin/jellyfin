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

#if __MonoCS__
            return EncryptStringUniversal(value);
#endif

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

#if __MonoCS__
            return DecryptStringUniversal(value);
#endif

            return Encoding.Default.GetString(ProtectedData.Unprotect(Encoding.Default.GetBytes(value), null, DataProtectionScope.LocalMachine));
        }

        private string EncryptStringUniversal(string value)
        {
            // Yes, this isn't good, but ProtectedData in mono is throwing exceptions, so use this for now

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        private string DecryptStringUniversal(string value)
        {
            // Yes, this isn't good, but ProtectedData in mono is throwing exceptions, so use this for now

            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
