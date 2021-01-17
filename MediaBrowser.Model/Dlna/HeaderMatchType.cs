namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the HeaderMatchType.
    /// </summary>
    public enum HeaderMatchType
    {
        /// <summary>
        /// Defines the match type of Equals.
        /// </summary>
        Equals = 0,

        /// <summary>
        /// Defines the match type of regex.
        /// </summary>
        Regex = 1,

        /// <summary>
        /// Defines the match type of substring.
        /// </summary>
        Substring = 2
    }
}
