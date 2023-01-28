namespace Jellyfin.Data.Enums;

using System;

/// <summary>
/// Used to set the types of full text searching.
/// </summary>
public enum FullTextSearchType
{
    /// <summary>
    /// Search by phrase.
    /// </summary>
    Phrase,

    /// <summary>
    /// Search by prefix.
    /// </summary>
    Prefix,

    /// <summary>
    /// Search by keyword.
    /// </summary>
    Keyword,
}
