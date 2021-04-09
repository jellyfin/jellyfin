namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Registered http client names.
    /// </summary>
    public static class NamedClient
    {
        /// <summary>
        /// Gets the value for the default named http client.
        /// </summary>
        public const string Default = nameof(Default);

        /// <summary>
        /// Gets the value for the MusicBrainz named http client.
        /// </summary>
        public const string MusicBrainz = nameof(MusicBrainz);

        /// <summary>
        /// Happy eyeballs implementation (Ip6 with fallback to IPv4).
        /// </summary>
        public const string HappyEyeballs = nameof(HappyEyeballs);
    }
}
