#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Implementations.MatchCriteria;

/// <summary>
/// Matches folders containing descendants with a specific media stream type and language.
/// </summary>
/// <param name="StreamType">The type of media stream to match (Audio, Subtitle, etc.).</param>
/// <param name="Language">List of languages to match.</param>
/// <param name="IsExternal">If not null, filters by internal (false) or external (true) streams. Only applicable to subtitles.</param>
public sealed record HasMediaStreamType(
    MediaStreamTypeEntity StreamType,
    IReadOnlyCollection<string> Language,
    bool? IsExternal = null) : FolderMatchCriteria
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HasMediaStreamType"/> class.
    /// </summary>
    /// <param name="StreamType">The type of media stream to match (Audio, Subtitle, etc.).</param>
    /// <param name="Language">The language to match.</param>
    /// <param name="IsExternal">If not null, filters by internal (false) or external (true) streams. Only applicable to subtitles.</param>
    public HasMediaStreamType(
        MediaStreamTypeEntity StreamType,
        string Language,
        bool? IsExternal = null) : this(StreamType, [Language], IsExternal)
    {
    }
}
