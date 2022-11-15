namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Enum PlayCommand.
    /// </summary>
    public enum PlayCommand
    {
        /// <summary>
        /// The play now.
        /// </summary>
        PlayNow = 0,

        /// <summary>
        /// The play next.
        /// </summary>
        PlayNext = 1,

        /// <summary>
        /// The play last.
        /// </summary>
        PlayLast = 2,

        /// <summary>
        /// The play instant mix.
        /// </summary>
        PlayInstantMix = 3,

        /// <summary>
        /// The play shuffle.
        /// </summary>
        PlayShuffle = 4
    }
}
