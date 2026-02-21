using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Implementations.MatchCriteria;

/// <summary>
/// Matches folders containing descendants with a specific media stream type and language.
/// </summary>
/// <param name="StreamType">The type of media stream to match (Audio, Subtitle, etc.).</param>
/// <param name="Language">The language to match.</param>
/// <param name="IsExternal">If not null, filters by internal (false) or external (true) streams. Only applicable to subtitles.</param>
public sealed record HasMediaStreamType(
    MediaStreamTypeEntity StreamType,
    string Language,
    bool? IsExternal = null) : FolderMatchCriteria;
