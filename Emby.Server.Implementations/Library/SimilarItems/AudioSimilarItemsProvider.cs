using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;

namespace Emby.Server.Implementations.Library.SimilarItems;

/// <summary>
/// Provides similar items for audio tracks.
/// </summary>
public class AudioSimilarItemsProvider : ILocalSimilarItemsProvider<Audio>
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioSimilarItemsProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    public AudioSimilarItemsProvider(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public string Name => "Local Genre/Tag";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.LocalSimilarityProvider;

    /// <inheritdoc/>
    public Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(Audio item, SimilarItemsQuery query, CancellationToken cancellationToken)
    {
        var internalQuery = new InternalItemsQuery(query.User)
        {
            Genres = item.Genres,
            Tags = item.Tags,
            Limit = query.Limit,
            DtoOptions = query.DtoOptions ?? new DtoOptions(),
            ExcludeItemIds = [.. query.ExcludeItemIds],
            ExcludeArtistIds = [.. query.ExcludeArtistIds],
            IncludeItemTypes = [BaseItemKind.Audio],
            EnableGroupByMetadataKey = false,
            EnableTotalRecordCount = true,
            OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)]
        };

        return Task.FromResult(_libraryManager.GetItemList(internalQuery));
    }
}
