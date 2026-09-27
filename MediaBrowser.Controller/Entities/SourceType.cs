namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// The source of an item.
    /// </summary>
    public enum SourceType
    {
        /// <summary>
        /// The item comes from a library.
        /// </summary>
        Library = 0,

        /// <summary>
        /// The item comes from a channel.
        /// </summary>
        Channel = 1,

        /// <summary>
        /// The item comes from live TV.
        /// </summary>
        LiveTV = 2
    }
}
