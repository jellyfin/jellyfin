namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// The type of a media source.
    /// </summary>
    public enum MediaSourceType
    {
        /// <summary>
        /// A default media source.
        /// </summary>
        Default = 0,

        /// <summary>
        /// A grouping of media sources.
        /// </summary>
        Grouping = 1,

        /// <summary>
        /// A placeholder media source, for example a disc that has to be inserted.
        /// </summary>
        Placeholder = 2
    }
}
