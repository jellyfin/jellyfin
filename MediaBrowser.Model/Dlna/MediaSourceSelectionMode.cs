namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines how the server selects the optimal media source when multiple versions are available.
    /// </summary>
    public enum MediaSourceSelectionMode
    {
        /// <summary>
        /// Default behavior - prefer highest quality, sorted by resolution descending.
        /// This may result in transcoding if the highest quality source is not directly playable.
        /// </summary>
        HighestQuality = 0,

        /// <summary>
        /// Prefer direct-playable sources over higher quality sources that require transcoding.
        /// Among direct-playable sources, prefer highest quality.
        /// </summary>
        PreferDirectPlay = 1,

        /// <summary>
        /// Network-aware selection. Considers available bandwidth when selecting between
        /// direct-playable sources. Will use a lower quality direct-playable source rather
        /// than transcoding a higher quality source when bandwidth is limited.
        /// </summary>
        NetworkAware = 2
    }
}
