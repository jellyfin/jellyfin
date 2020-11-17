namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Data object used to pass result of name and year parsing.
    /// </summary>
    public struct AudioBookNameParserResult
    {
        /// <summary>
        /// Gets or sets name of audiobook.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Year { get; set; }
    }
}
