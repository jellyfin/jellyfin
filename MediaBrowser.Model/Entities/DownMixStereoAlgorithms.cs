namespace MediaBrowser.Model.Entities;

/// <summary>
/// An enum representing an algorithm to downmix 6ch+ to stereo.
/// Algorithms sourced from https://superuser.com/questions/852400/properly-downmix-5-1-to-stereo-using-ffmpeg/1410620#1410620.
/// </summary>
public enum DownMixStereoAlgorithms
{
    /// <summary>
    /// No special algorithm.
    /// </summary>
    None = 0,

    /// <summary>
    /// Algorithm by Dave_750.
    /// </summary>
    Dave750 = 1,

    /// <summary>
    /// Nightmode Dialogue algorithm.
    /// </summary>
    NightmodeDialogue = 2
}
