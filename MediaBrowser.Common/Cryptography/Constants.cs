namespace MediaBrowser.Common.Cryptography
{
    /// <summary>
    /// Class containing global constants for Jellyfin Cryptography.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The default length for new salts.
        /// </summary>
        public const int DefaultSaltLength = 64;

        /// <summary>
        /// The default amount of iterations for hashing passwords.
        /// </summary>
        public const int DefaultIterations = 1000;
    }
}
