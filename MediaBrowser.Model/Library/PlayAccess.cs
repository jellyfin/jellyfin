namespace MediaBrowser.Model.Library
{
    /// <summary>
    /// The play access of an item.
    /// </summary>
    public enum PlayAccess
    {
        /// <summary>
        /// The item can be played.
        /// </summary>
        Full = 0,

        /// <summary>
        /// The item cannot be played.
        /// </summary>
        None = 1
    }
}
