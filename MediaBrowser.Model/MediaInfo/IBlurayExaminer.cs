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
    /// Gets the clips of the main playlist of a Blu-ray ISO image.
    /// </summary>
    /// <param name="path">The path to the image.</param>
    /// <returns>The clips in playback order, empty if the image has no usable playlist.</returns>
    IReadOnlyList<BlurayIsoClip> GetIsoClips(string path);
}
