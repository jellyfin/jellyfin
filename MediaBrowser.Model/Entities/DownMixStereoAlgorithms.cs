namespace MediaBrowser.Model.Entities;

/// <summary>
/// An enum representing an algorithm to downmix surround sound to stereo.
/// </summary>
public enum DownMixStereoAlgorithms
{
    /// <summary>
    /// No special algorithm.
    /// </summary>
    None = 0,

    /// <summary>
    /// Algorithm by Dave_750.
    /// Sourced from https://superuser.com/questions/852400/properly-downmix-5-1-to-stereo-using-ffmpeg/1410620#1410620.
    /// </summary>
    Dave750 = 1,

    /// <summary>
    /// Nightmode Dialogue algorithm.
    /// Sourced from https://superuser.com/questions/852400/properly-downmix-5-1-to-stereo-using-ffmpeg/1410620#1410620.
    /// </summary>
    NightmodeDialogue = 2,

    /// <summary>
    /// RFC7845 Section 5.1.1.5 defined algorithm.
    /// </summary>
    Rfc7845 = 3,

    /// <summary>
    /// AC-4 standard algorithm with its default gain values.
    /// Defined in ETSI TS 103 190 Section 6.2.17.
    /// </summary>
    Ac4 = 4
}
