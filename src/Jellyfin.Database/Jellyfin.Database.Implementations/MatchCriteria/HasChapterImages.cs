namespace Jellyfin.Database.Implementations.MatchCriteria;

/// <summary>
/// Matches folders containing descendants with chapter images.
/// </summary>
public sealed record HasChapterImages : FolderMatchCriteria;
