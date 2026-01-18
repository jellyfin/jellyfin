using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;

namespace Emby.Server.Implementations.Library.SimilarItems;

/// <summary>
/// Provides similar items for movies and trailers.
/// </summary>
public class MovieSimilarItemsProvider : ILocalSimilarItemsProvider<Movie>, ILocalSimilarItemsProvider<Trailer>
{
    private readonly ILibraryManager _libraryManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieSimilarItemsProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    public MovieSimilarItemsProvider(
        ILibraryManager libraryManager,
        IServerConfigurationManager serverConfigurationManager)
    {
        _libraryManager = libraryManager;
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc/>
    public string Name => "Local Genre/Tag";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.LocalSimilarityProvider;

    /// <inheritdoc/>
    public Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(Movie item, SimilarItemsQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSimilarMovieItems(item, query));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(Trailer item, SimilarItemsQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSimilarMovieItems(item, query));
    }

    private IReadOnlyList<BaseItem> GetSimilarMovieItems(BaseItem item, SimilarItemsQuery query)
    {
        var includeItemTypes = new List<BaseItemKind> { BaseItemKind.Movie };

        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            includeItemTypes.Add(BaseItemKind.Trailer);
            includeItemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        var internalQuery = new InternalItemsQuery(query.User)
        {
            Genres = item.Genres,
            Tags = item.Tags,
            Limit = query.Limit,
            DtoOptions = query.DtoOptions ?? new DtoOptions(),
            ExcludeItemIds = [.. query.ExcludeItemIds],
            IncludeItemTypes = [.. includeItemTypes],
            EnableGroupByMetadataKey = true,
            EnableTotalRecordCount = false,
            OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)]
        };

        return _libraryManager.GetItemList(internalQuery);
    }
}
