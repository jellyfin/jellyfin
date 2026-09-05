using System;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Providers.Sqlite.Entities;

/// <summary>
/// Entity mapped to the BaseItems_fts FTS5 virtual table.
/// This enables full-text search capabilities through LINQ queries without raw SQL.
/// </summary>
/// <remarks>
/// Based on the approach from https://www.bricelam.net/2020/08/08/sqlite-fts-and-efcore.html
/// The Match and Rank properties are special FTS5 columns used for searching and ranking.
/// </remarks>
public class BaseItemFtsEntity
{
    /// <summary>
    /// Gets or sets the rowid from the FTS5 table (maps to BaseItems rowid).
    /// </summary>
    public long RowId { get; set; }

    /// <summary>
    /// Gets or sets the FTS5 MATCH column for full-text search queries.
    /// Usage: Where(f => f.Match == "search term")
    /// This column has the same name as the FTS table for proper FTS5 matching.
    /// </summary>
    [Column("BaseItems_fts")]
    public string? Match { get; set; }

    /// <summary>
    /// Gets or sets the FTS5 rank() for result ordering.
    /// Lower (more negative) values = better matches.
    /// Can be customized with column weights using bm25() function.
    /// </summary>
    public double Rank { get; set; }

    /// <summary>
    /// Gets or sets the Id from the indexed content.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the original BaseItem.
    /// </summary>
    public BaseItemEntity? BaseItem { get; set; }
}
