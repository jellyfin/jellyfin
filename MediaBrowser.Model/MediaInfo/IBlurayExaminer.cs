using System.Collections.Generic;

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
    /// Gets all available playback titles from a Blu-ray disc or ISO, ordered by title number.
    /// </summary>
    /// <param name="path">The path to the disc directory or ISO file.</param>
    /// <returns>A list of <see cref="IsoTitleInfo"/> with title numbers and durations.</returns>
    IReadOnlyList<IsoTitleInfo> GetTitles(string path);
}
