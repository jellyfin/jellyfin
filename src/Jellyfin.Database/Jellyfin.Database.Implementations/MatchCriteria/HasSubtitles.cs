namespace Jellyfin.Database.Implementations.MatchCriteria;

/// <summary>
/// Matches folders containing descendants with subtitles.
/// </summary>
public sealed record HasSubtitles : FolderMatchCriteria;
