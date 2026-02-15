using System;

namespace MediaBrowser.Model.MediaInfo;

/// <summary>
/// How is the audio index determined.
/// </summary>
[Flags]
public enum AudioIndexSource
{
    /// <summary>
    /// The default index when no preference is specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// The index is calculated whether the track is marked as default or not.
    /// </summary>
    Default = 1 << 0,

    /// <summary>
    /// The index is calculated whether the track is in preferred language or not.
    /// </summary>
    Language = 1 << 1,

    /// <summary>
    /// The index is specified by the user.
    /// </summary>
    User = 1 << 2
}
