
namespace MediaBrowser.Controller.Security
{
    public interface IEncryptionManager
    {
        /// <summary>
        /// Encrypts the string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        string EncryptString(string value);

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        string DecryptString(string value);
    }
}
