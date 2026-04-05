namespace MediaBrowser.Model.MediaInfo;

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

    /// <summary>
    /// Gets the disc info for a specific playlist.
    /// </summary>
    /// <param name="path">The disc root path.</param>
    /// <param name="playlistName">The playlist filename (e.g. "00202.mpls").</param>
    /// <returns>BlurayDiscInfo.</returns>
    BlurayDiscInfo GetDiscInfo(string path, string playlistName)
        => GetDiscInfo(path);
}
