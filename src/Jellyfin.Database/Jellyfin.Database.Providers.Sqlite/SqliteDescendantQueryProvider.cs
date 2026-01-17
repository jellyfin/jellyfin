using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.MatchCriteria;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// SQLite implementation of descendant queries using optimized ancestor lookups.
/// Uses AncestorIds and LinkedChildren tables for efficient parent-child traversal.
/// </summary>
public class SqliteDescendantQueryProvider : IDescendantQueryProvider
{
    /// <summary>
    /// Recursive CTE fragment that traverses UP the tree from matching items to find all ancestor folders.
    /// Expects a preceding CTE named "MatchingItems" with an ItemId column.
    /// </summary>
    private const string AllAncestorsCte = """
        AllAncestors AS (
            SELECT a.ParentItemId AS AncestorId
            FROM AncestorIds a
            WHERE a.ItemId IN (SELECT ItemId FROM MatchingItems)
            UNION
            SELECT lc.ParentId AS AncestorId
            FROM LinkedChildren lc
            WHERE lc.ChildId IN (SELECT ItemId FROM MatchingItems)
            UNION
            SELECT a.ParentItemId AS AncestorId
            FROM AllAncestors aa
            INNER JOIN AncestorIds a ON a.ItemId = aa.AncestorId
            UNION
            SELECT lc.ParentId AS AncestorId
            FROM AllAncestors aa
            INNER JOIN LinkedChildren lc ON lc.ChildId = aa.AncestorId
        )
        SELECT DISTINCT AncestorId AS Value FROM AllAncestors
        """;

    /// <inheritdoc />
    public IQueryable<Guid> GetAllDescendantIds(JellyfinDbContext context, Guid parentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sql = """
            WITH RECURSIVE AllDescendants AS (
                SELECT ItemId FROM AncestorIds WHERE ParentItemId = {0}
                UNION
                SELECT ChildId AS ItemId FROM LinkedChildren WHERE ParentId = {0}
                UNION ALL
                SELECT a.ItemId
                FROM AllDescendants d
                INNER JOIN BaseItems b ON b.Id = d.ItemId AND b.IsFolder = 1
                INNER JOIN AncestorIds a ON a.ParentItemId = d.ItemId
                UNION ALL
                SELECT lc.ChildId AS ItemId
                FROM AllDescendants d
                INNER JOIN BaseItems b ON b.Id = d.ItemId AND b.IsFolder = 1
                INNER JOIN LinkedChildren lc ON lc.ParentId = d.ItemId
            )
            SELECT DISTINCT ItemId AS Value FROM AllDescendants
            """;

        return context.Database.SqlQueryRaw<Guid>(sql, parentId);
    }

    /// <inheritdoc />
    public IQueryable<Guid> GetFolderIdsMatching(JellyfinDbContext context, FolderMatchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(criteria);

        return criteria switch
        {
            HasSubtitles => GetFolderIdsWithSubtitles(context),
            HasChapterImages => GetFolderIdsWithChapterImages(context),
            HasMediaStreamType m => GetFolderIdsWithMediaStream(context, m.StreamType, m.Language, m.IsExternal),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), $"Unknown criteria type: {criteria.GetType().Name}")
        };
    }

    private IQueryable<Guid> GetFolderIdsWithSubtitles(JellyfinDbContext context)
    {
        var sql = $"""
            WITH RECURSIVE MatchingItems AS (
                SELECT DISTINCT ms.ItemId FROM MediaStreamInfos ms WHERE ms.StreamType = 2
            ),
            {AllAncestorsCte}
            """;

        return context.Database.SqlQueryRaw<Guid>(sql);
    }

    private IQueryable<Guid> GetFolderIdsWithChapterImages(JellyfinDbContext context)
    {
        var sql = $"""
            WITH RECURSIVE MatchingItems AS (
                SELECT DISTINCT c.ItemId FROM Chapters c WHERE c.ImagePath IS NOT NULL
            ),
            {AllAncestorsCte}
            """;

        return context.Database.SqlQueryRaw<Guid>(sql);
    }

    private IQueryable<Guid> GetFolderIdsWithMediaStream(JellyfinDbContext context, MediaStreamTypeEntity streamType, string language, bool? isExternal)
    {
        ArgumentNullException.ThrowIfNull(language);

        var streamTypeInt = (int)streamType;
        var externalCondition = isExternal switch
        {
            true => " AND ms.IsExternal = 1",
            false => " AND ms.IsExternal = 0",
            null => string.Empty
        };

        var sql = $$"""
            WITH RECURSIVE MatchingItems AS (
                SELECT DISTINCT ms.ItemId FROM MediaStreamInfos ms
                WHERE ms.StreamType = {0} AND ms.Language = {1}{{externalCondition}}
            ),
            {{AllAncestorsCte}}
            """;

        return context.Database.SqlQueryRaw<Guid>(sql, streamTypeInt, language);
    }
}
