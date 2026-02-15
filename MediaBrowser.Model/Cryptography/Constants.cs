namespace MediaBrowser.Model.Cryptography
{
    /// <summary>
    /// Class containing global constants for Jellyfin Cryptography.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The default length for new salts.
        /// </summary>
        public const int DefaultSaltLength = 128 / 8;

        /// <summary>
        /// The default output length.
        /// </summary>
        public const int DefaultOutputLength = 512 / 8;

        /// <summary>
        /// The default amount of iterations for hashing passwords.
        /// </summary>
        public const int DefaultIterations = 210000;
    }
}
