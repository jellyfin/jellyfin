using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalItemsQuery.AncestorIds"/>, the filter every recursive query under a parent runs on.
/// </summary>
public sealed class BaseItemRepositoryAncestorFilterTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private const string SeasonKey = "series-key-001";

    private readonly CommandRecorder _recorder;
    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _series = Guid.NewGuid();
    private readonly Guid _season = Guid.NewGuid();
    private readonly Guid _episode = Guid.NewGuid();
    private readonly Guid _otherSeries = Guid.NewGuid();
    private readonly Guid _otherEpisode = Guid.NewGuid();
    private readonly Guid _emptyFolder = Guid.NewGuid();

    public BaseItemRepositoryAncestorFilterTests()
        : this(new CommandRecorder())
    {
    }

    private BaseItemRepositoryAncestorFilterTests(CommandRecorder recorder)
        : base(recorder)
    {
        _recorder = recorder;
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void AncestorIds_SingleAncestor_ReturnsOnlyItsDescendants()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { AncestorIds = [_series] });

        Assert.Equal(new[] { _season, _episode }.Order(), ids.Order());
    }

    [Fact]
    public void AncestorIds_OverlappingAncestors_ReturnsUnionWithoutDuplicates()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            AncestorIds = [_library, _series, _otherSeries],
            IncludeItemTypes = [BaseItemKind.Episode]
        });

        Assert.Equal(new[] { _episode, _otherEpisode }.Order(), ids.Order());
    }

    [Fact]
    public void AncestorIds_ParentWithoutDescendants_ReturnsNothing()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { AncestorIds = [_emptyFolder] });

        Assert.Empty(ids);
    }

    [Fact]
    public void AncestorWithPresentationUniqueKey_ReturnsDescendantsOfTheKeyedFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            AncestorWithPresentationUniqueKey = SeasonKey,
            IncludeItemTypes = [BaseItemKind.Episode]
        });

        Assert.Equal([_episode], ids);
    }

    [Fact]
    public void AncestorIds_SeeksDescendantsByParent()
    {
        _recorder.Commands.Clear();

        _repository.GetItemList(new InternalItemsQuery
        {
            AncestorIds = [_emptyFolder],
            OrderBy = [(ItemSortBy.IsFolder, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending)],
            Limit = 1
        });

        var query = Assert.Single(_recorder.Commands, c => c.Sql.Contains("\"AncestorIds\"", StringComparison.Ordinal));
        Assert.Contains(Explain(query), line => line.Contains("IX_AncestorIds_ParentItemId (ParentItemId=?)", StringComparison.Ordinal));
    }

    private void Seed(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = FolderType, Name = "Shows", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _series, Type = SeriesType, Name = "Series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _season, Type = SeasonType, Name = "Season 1", IsFolder = true, PresentationUniqueKey = SeasonKey });
        context.BaseItems.Add(new BaseItemEntity { Id = _episode, Type = EpisodeType, Name = "Episode 1" });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherSeries, Type = SeriesType, Name = "Other series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherEpisode, Type = EpisodeType, Name = "Other episode" });
        context.BaseItems.Add(new BaseItemEntity { Id = _emptyFolder, Type = FolderType, Name = "Empty", IsFolder = true });

        // AncestorIds is a closure: production writes one row per ancestor, not just the parent.
        AddAncestors(context, _series, _library);
        AddAncestors(context, _season, _series, _library);
        AddAncestors(context, _episode, _season, _series, _library);
        AddAncestors(context, _otherSeries, _library);
        AddAncestors(context, _otherEpisode, _otherSeries, _library);
        AddAncestors(context, _emptyFolder, _library);

        context.SaveChanges();
    }

    private static void AddAncestors(JellyfinDbContext context, Guid itemId, params Guid[] ancestorIds)
    {
        foreach (var ancestorId in ancestorIds)
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = ancestorId,
                Item = null!,
                ParentItem = null!
            });
        }
    }

    private string[] Explain(RecordedCommand query)
    {
        using var context = CreateDbContext();
        using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // query.Sql is generated by EF Core; query values remain bound parameters.
        command.CommandText = "EXPLAIN QUERY PLAN " + query.Sql;
#pragma warning restore CA2100
        foreach (var value in query.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = value.Name;
            parameter.Value = value.Value;
            command.Parameters.Add(parameter);
        }

        using var reader = command.ExecuteReader();
        var plan = new List<string>();
        while (reader.Read())
        {
            plan.Add(reader.GetString(3));
        }

        return plan.ToArray();
    }

    private sealed record RecordedCommand(string Sql, (string Name, object? Value)[] Parameters);

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Commands.Add(new RecordedCommand(
                command.CommandText,
                command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, p.Value)).ToArray()));
            return result;
        }
    }
}
