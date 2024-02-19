namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Track selection policy when track switching is not supported.
    /// </summary>
    public enum SingleAudioPolicy
    {
        /// <summary>
        /// Selects the first track.
        /// </summary>
        First = 0,

        /// <summary>
        /// Selects the first supported track.
        /// </summary>
        FirstSupported = 1,

        /// <summary>
        /// Selects the track marked 'Default'.
        /// </summary>
        Default = 2,

        /// <summary>
        /// Selects the supported track marked 'Default'.
        /// </summary>
        DefaultSupported = 3
    }
}
