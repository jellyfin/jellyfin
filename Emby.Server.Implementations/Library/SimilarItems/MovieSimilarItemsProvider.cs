using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;

namespace Emby.Server.Implementations.Library.SimilarItems;

/// <summary>
/// Provides similar items for movies and trailers using weighted scoring.
/// </summary>
public sealed class MovieSimilarItemsProvider : ILocalSimilarItemsProvider<Movie>, ILocalSimilarItemsProvider<Trailer>, IBatchLocalSimilarItemsProvider
{
    private const int GenreWeight = 10;
    private const int TagWeight = 5;
    private const int StudioWeight = 5;
    private const int DirectorWeight = 50;
    private const int ActorWeight = 15;

    // Caps the batch fan-out so downstream IN-list sizes (per-source scores, accessible-id
    // load, navigation includes) stay bounded regardless of caller input.
    private const int MaxBatchSourceItems = 64;

    private static readonly (ItemValueType Type, int Weight)[] _itemValueDimensions =
    [
        (ItemValueType.Genre, GenreWeight),
        (ItemValueType.Tags, TagWeight),
        (ItemValueType.Studios, StudioWeight)
    ];

    private static readonly Dictionary<string, int> _personTypeWeights = new(StringComparer.Ordinal)
    {
        [nameof(PersonKind.Director)] = DirectorWeight,
        [nameof(PersonKind.Actor)] = ActorWeight,
        [nameof(PersonKind.GuestStar)] = ActorWeight,
    };

    private static readonly string[] _scoredPersonTypes = [.. _personTypeWeights.Keys];

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemQueryHelpers _queryHelpers;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieSimilarItemsProvider"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="queryHelpers">The shared query helpers.</param>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    public MovieSimilarItemsProvider(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemQueryHelpers queryHelpers,
        IServerConfigurationManager serverConfigurationManager,
        ILibraryManager libraryManager)
    {
        _dbProvider = dbProvider;
        _queryHelpers = queryHelpers;
        _serverConfigurationManager = serverConfigurationManager;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public string Name => "Local Genre/Tag";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.LocalSimilarityProvider;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BaseItemDto>> GetSimilarItemsAsync(Movie item, SimilarItemsQuery query, CancellationToken cancellationToken)
    {
        var results = await GetBatchSimilarItemsAsync([item], query, cancellationToken).ConfigureAwait(false);
        return results.TryGetValue(item.Id, out var items) ? items : [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BaseItemDto>> GetSimilarItemsAsync(Trailer item, SimilarItemsQuery query, CancellationToken cancellationToken)
    {
        var results = await GetBatchSimilarItemsAsync([item], query, cancellationToken).ConfigureAwait(false);
        return results.TryGetValue(item.Id, out var items) ? items : [];
    }

    bool ILocalSimilarItemsProvider.Supports(Type itemType)
        => typeof(Movie).IsAssignableFrom(itemType) || typeof(Trailer).IsAssignableFrom(itemType);

    Task<IReadOnlyList<BaseItem>> ILocalSimilarItemsProvider.GetSimilarItemsAsync(BaseItem item, SimilarItemsQuery query, CancellationToken cancellationToken)
        => item switch
        {
            Movie movie => GetSimilarItemsAsync(movie, query, cancellationToken),
            Trailer trailer => GetSimilarItemsAsync(trailer, query, cancellationToken),
            _ => throw new ArgumentException($"Unsupported item type {item.GetType()}", nameof(item))
        };

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, IReadOnlyList<BaseItemDto>>> GetBatchSimilarItemsAsync(
        IReadOnlyList<BaseItemDto> sourceItems,
        SimilarItemsQuery query,
        CancellationToken cancellationToken)
    {
        var includeItemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            includeItemTypes.Add(BaseItemKind.Trailer);
            includeItemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        var limit = query.Limit ?? 50;
        var dtoOptions = query.DtoOptions ?? new DtoOptions();

        if (sourceItems.Count > MaxBatchSourceItems)
        {
            sourceItems = sourceItems.Take(MaxBatchSourceItems).ToList();
        }

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // Phase 1: Score all candidates per source item
            var sourceIds = sourceItems.Select(i => i.Id).ToList();
            var perSourceScores = await ComputeBatchScoresAsync(sourceIds, context, cancellationToken).ConfigureAwait(false);

            var allCandidateIds = new HashSet<Guid>();
            foreach (var (_, scores) in perSourceScores)
            {
                allCandidateIds.UnionWith(
                    scores.OrderByDescending(kvp => kvp.Value)
                        .Take(limit * 3)
                        .Select(kvp => kvp.Key));
            }

            var result = new Dictionary<Guid, IReadOnlyList<BaseItemDto>>();
            if (allCandidateIds.Count == 0)
            {
                return result;
            }

            // Phase 2: One access filter for all candidates
            var filter = new InternalItemsQuery(query.User)
            {
                IncludeItemTypes = [.. includeItemTypes],
                ExcludeItemIds = [.. query.ExcludeItemIds],
                DtoOptions = dtoOptions,
                EnableGroupByMetadataKey = true,
                EnableTotalRecordCount = false,
                IsMovie = true,
                IsPlayed = false
            };

            if (query.User is not null)
            {
                _libraryManager.ConfigureUserAccess(filter, query.User);
            }

            _queryHelpers.PrepareFilterQuery(filter);
            var baseQuery = _queryHelpers.PrepareItemQuery(context, filter);
            baseQuery = _queryHelpers.TranslateQuery(baseQuery, context, filter);

            var allCandidateIdsList = allCandidateIds.ToList();
            var accessibleItems = await baseQuery
                .WhereOneOrMany(allCandidateIdsList, e => e.Id)
                .Select(e => new { e.Id, e.PresentationUniqueKey })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            // Phase 3: Pick top IDs per source, dedup by PresentationUniqueKey
            var allOrderedIds = new HashSet<Guid>();
            var perSourceOrderedIds = new Dictionary<Guid, List<Guid>>();

            foreach (var item in sourceItems)
            {
                if (!perSourceScores.TryGetValue(item.Id, out var scores))
                {
                    continue;
                }

                var orderedIds = accessibleItems
                    .Where(x => scores.ContainsKey(x.Id))
                    .OrderByDescending(x => scores.GetValueOrDefault(x.Id))
                    .DistinctBy(x => x.PresentationUniqueKey)
                    .Take(limit)
                    .Select(x => x.Id)
                    .ToList();

                if (orderedIds.Count > 0)
                {
                    perSourceOrderedIds[item.Id] = orderedIds;
                    allOrderedIds.UnionWith(orderedIds);
                }
            }

            if (allOrderedIds.Count == 0)
            {
                return result;
            }

            // Phase 4: One entity load for all results
            var allOrderedIdsList = allOrderedIds.ToList();
            var entities = await _queryHelpers.ApplyNavigations(
                    context.BaseItems.AsNoTracking().WhereOneOrMany(allOrderedIdsList, e => e.Id),
                    filter)
                .AsSplitQuery()
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entitiesById = entities
                .Select(e => _queryHelpers.DeserializeBaseItem(e, filter.SkipDeserialization))
                .Where(dto => dto is not null)
                .ToDictionary(i => i!.Id);

            // Phase 5: Split by source, preserving score order
            foreach (var (sourceId, orderedIds) in perSourceOrderedIds)
            {
                var items = orderedIds
                    .Where(entitiesById.ContainsKey)
                    .Select(id => entitiesById[id]!)
                    .ToList();

                if (items.Count > 0)
                {
                    result[sourceId] = items;
                }
            }

            return result;
        }
    }

    private static async Task<Dictionary<Guid, Dictionary<Guid, int>>> ComputeBatchScoresAsync(List<Guid> sourceIds, JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Dictionary<Guid, int>>();
        foreach (var id in sourceIds)
        {
            result[id] = [];
        }

        foreach (var (valueType, weight) in _itemValueDimensions)
        {
            var sourceRows = await context.ItemValuesMap.AsNoTracking()
                .Where(m => sourceIds.Contains(m.ItemId) && m.ItemValue.Type == valueType)
                .Select(m => new { m.ItemId, Key = m.ItemValue.CleanValue })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var sourceMap = sourceRows.GroupBy(r => r.ItemId).ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToHashSet());
            var allKeys = sourceMap.Values.SelectMany(v => v).Distinct().ToList();
            if (allKeys.Count == 0)
            {
                continue;
            }

            var candidateRows = await context.ItemValuesMap.AsNoTracking()
                .Where(m => m.ItemValue.Type == valueType && allKeys.Contains(m.ItemValue.CleanValue))
                .Select(m => new { m.ItemId, Key = m.ItemValue.CleanValue })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var keyToCandidates = candidateRows.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
            ApplyDimensionScores(sourceIds, sourceMap, keyToCandidates, weight, result);
        }

        var personSourceRows = await context.PeopleBaseItemMap.AsNoTracking()
            .Where(m => sourceIds.Contains(m.ItemId) && _scoredPersonTypes.Contains(m.People.PersonType))
            .Select(m => new { m.ItemId, m.PeopleId, m.People.PersonType })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (personSourceRows.Count > 0)
        {
            var personCandidateRows = await context.PeopleBaseItemMap.AsNoTracking()
                .Where(m => context.PeopleBaseItemMap
                    .Where(s => sourceIds.Contains(s.ItemId) && _scoredPersonTypes.Contains(s.People.PersonType))
                    .Select(s => s.PeopleId)
                    .Contains(m.PeopleId))
                .Select(m => new { m.ItemId, m.PeopleId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var personToCandidates = personCandidateRows
                .GroupBy(r => r.PeopleId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());

            foreach (var weightGroup in personSourceRows.GroupBy(r => _personTypeWeights[r.PersonType!]))
            {
                var sourceMap = weightGroup
                    .GroupBy(r => r.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.PeopleId).ToHashSet());
                ApplyDimensionScores(sourceIds, sourceMap, personToCandidates, weightGroup.Key, result);
            }
        }

        foreach (var sourceId in sourceIds)
        {
            var scoreMap = result[sourceId];
            scoreMap.Remove(sourceId);
            if (scoreMap.Count == 0)
            {
                result.Remove(sourceId);
            }
        }

        return result;
    }

    private static void ApplyDimensionScores<TKey>(
        List<Guid> sourceIds,
        Dictionary<Guid, HashSet<TKey>> sourceMap,
        Dictionary<TKey, List<Guid>> keyToCandidates,
        int weight,
        Dictionary<Guid, Dictionary<Guid, int>> result)
        where TKey : notnull
    {
        foreach (var sourceId in sourceIds)
        {
            if (!sourceMap.TryGetValue(sourceId, out var sourceKeys))
            {
                continue;
            }

            var scoreMap = result[sourceId];
            foreach (var key in sourceKeys)
            {
                if (!keyToCandidates.TryGetValue(key, out var candidates))
                {
                    continue;
                }

                foreach (var candidateId in candidates)
                {
                    scoreMap[candidateId] = scoreMap.GetValueOrDefault(candidateId) + weight;
                }
            }
        }
    }
}
