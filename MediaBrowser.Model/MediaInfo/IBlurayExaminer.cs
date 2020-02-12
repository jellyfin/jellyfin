namespace MediaBrowser.Model.MediaInfo
{
    /// <summary>
    /// Interface IBlurayExaminer.
    /// </summary>
    public interface IBlurayExaminer
    {
        /// <summary>
        /// Gets the disc info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BlurayDiscInfo.</returns>
        BlurayDiscInfo GetDiscInfo(string path);
    }
}
