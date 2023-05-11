namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Registered http client names.
    /// </summary>
    public static class NamedClient
    {
        /// <summary>
        /// Gets the value for the default named http client which implements happy eyeballs.
        /// </summary>
        public const string Default = nameof(Default);

        /// <summary>
        /// Gets the value for the MusicBrainz named http client.
        /// </summary>
        public const string MusicBrainz = nameof(MusicBrainz);

        /// <summary>
        /// Gets the value for the DLNA named http client.
        /// </summary>
        public const string Dlna = nameof(Dlna);

        /// <summary>
        /// Non happy eyeballs implementation.
        /// </summary>
        public const string DirectIp = nameof(DirectIp);
    }
}
