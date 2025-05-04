using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Search controller.
/// </summary>
[Route("Search/Hints")]
[Authorize]
public class SearchController : BaseJellyfinApiController
{
    private readonly ISearchEngine _searchEngine;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly IImageProcessor _imageProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchController"/> class.
    /// </summary>
    /// <param name="searchEngine">Instance of <see cref="ISearchEngine"/> interface.</param>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of <see cref="IDtoService"/> interface.</param>
    /// <param name="imageProcessor">Instance of <see cref="IImageProcessor"/> interface.</param>
    public SearchController(
        ISearchEngine searchEngine,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        IImageProcessor imageProcessor)
    {
        _searchEngine = searchEngine;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _imageProcessor = imageProcessor;
    }

    /// <summary>
    /// Gets the search hint result.
    /// </summary>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="userId">Optional. Supply a user id to search within a user's library or omit to search all.</param>
    /// <param name="searchTerm">The search term to filter on.</param>
    /// <param name="includeItemTypes">If specified, only results with the specified item types are returned. This allows multiple, comma delimited.</param>
    /// <param name="excludeItemTypes">If specified, results with these item types are filtered out. This allows multiple, comma delimited.</param>
    /// <param name="mediaTypes">If specified, only results with the specified media types are returned. This allows multiple, comma delimited.</param>
    /// <param name="parentId">If specified, only children of the parent are returned.</param>
    /// <param name="isMovie">Optional filter for movies.</param>
    /// <param name="isSeries">Optional filter for series.</param>
    /// <param name="isNews">Optional filter for news.</param>
    /// <param name="isKids">Optional filter for kids.</param>
    /// <param name="isSports">Optional filter for sports.</param>
    /// <param name="includePeople">Optional filter whether to include people.</param>
    /// <param name="includeMedia">Optional filter whether to include media.</param>
    /// <param name="includeGenres">Optional filter whether to include genres.</param>
    /// <param name="includeStudios">Optional filter whether to include studios.</param>
    /// <param name="includeArtists">Optional filter whether to include artists.</param>
    /// <response code="200">Search hint returned.</response>
    /// <returns>An <see cref="SearchHintResult"/> with the results of the search.</returns>
    [HttpGet]
    [Description("Gets search hints based on a search term")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<SearchHintResult> GetSearchHints(
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] Guid? userId,
        [FromQuery, Required] string searchTerm,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaTypes,
        [FromQuery] Guid? parentId,
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery] bool includePeople = true,
        [FromQuery] bool includeMedia = true,
        [FromQuery] bool includeGenres = true,
        [FromQuery] bool includeStudios = true,
        [FromQuery] bool includeArtists = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var result = _searchEngine.GetSearchHints(new SearchQuery
        {
            Limit = limit,
            SearchTerm = searchTerm,
            IncludeArtists = includeArtists,
            IncludeGenres = includeGenres,
            IncludeMedia = includeMedia,
            IncludePeople = includePeople,
            IncludeStudios = includeStudios,
            StartIndex = startIndex,
            UserId = userId.Value,
            IncludeItemTypes = includeItemTypes,
            ExcludeItemTypes = excludeItemTypes,
            MediaTypes = mediaTypes,
            ParentId = parentId,

            IsKids = isKids,
            IsMovie = isMovie,
            IsNews = isNews,
            IsSeries = isSeries,
            IsSports = isSports
        });

        return new SearchHintResult(result.Items.Select(GetSearchHintResult).ToArray(), result.TotalRecordCount);
    }

    /// <summary>
    /// Gets the search hint result.
    /// </summary>
    /// <param name="hintInfo">The hint info.</param>
    /// <returns>SearchHintResult.</returns>
    private SearchHint GetSearchHintResult(SearchHintInfo hintInfo)
    {
        var item = hintInfo.Item;

        var result = new SearchHint
        {
            Name = item.Name,
            IndexNumber = item.IndexNumber,
            ParentIndexNumber = item.ParentIndexNumber,
            Id = item.Id,
            Type = item.GetBaseItemKind(),
            MediaType = item.MediaType,
            MatchedTerm = hintInfo.MatchedTerm,
            RunTimeTicks = item.RunTimeTicks,
            ProductionYear = item.ProductionYear,
            ChannelId = item.ChannelId,
            EndDate = item.EndDate
        };

#pragma warning disable CS0618
        // Kept for compatibility with older clients
        result.ItemId = result.Id;
#pragma warning restore CS0618

        if (item.IsFolder)
        {
            result.IsFolder = true;
        }

        var primaryImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Primary);

        if (primaryImageTag is not null)
        {
            result.PrimaryImageTag = primaryImageTag;
            result.PrimaryImageAspectRatio = _dtoService.GetPrimaryImageAspectRatio(item);
        }

        SetThumbImageInfo(result, item);
        SetBackdropImageInfo(result, item);

        switch (item)
        {
            case IHasSeries hasSeries:
                result.Series = hasSeries.SeriesName;
                break;
            case LiveTvProgram program:
                result.StartDate = program.StartDate;
                break;
            case Series series:
                if (series.Status.HasValue)
                {
                    result.Status = series.Status.Value.ToString();
                }

                break;
            case MusicAlbum album:
                result.Artists = album.Artists;
                result.AlbumArtist = album.AlbumArtist;
                break;
            case Audio song:
                result.AlbumArtist = song.AlbumArtists?.FirstOrDefault();
                result.Artists = song.Artists;

                MusicAlbum musicAlbum = song.AlbumEntity;

                if (musicAlbum is not null)
                {
                    result.Album = musicAlbum.Name;
                    result.AlbumId = musicAlbum.Id;
                }
                else
                {
                    result.Album = song.Album;
                }

                break;
        }

        if (!item.ChannelId.IsEmpty())
        {
            var channel = _libraryManager.GetItemById<BaseItem>(item.ChannelId);
            result.ChannelName = channel?.Name;
        }

        return result;
    }

    private void SetThumbImageInfo(SearchHint hint, BaseItem item)
    {
        var itemWithImage = item.HasImage(ImageType.Thumb) ? item : null;

        if (itemWithImage is null && item is Episode)
        {
            itemWithImage = GetParentWithImage<Series>(item, ImageType.Thumb);
        }

        itemWithImage ??= GetParentWithImage<BaseItem>(item, ImageType.Thumb);

        if (itemWithImage is not null)
        {
            var tag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Thumb);

            if (tag is not null)
            {
                hint.ThumbImageTag = tag;
                hint.ThumbImageItemId = itemWithImage.Id.ToString("N", CultureInfo.InvariantCulture);
            }
        }
    }

    private void SetBackdropImageInfo(SearchHint hint, BaseItem item)
    {
        var itemWithImage = (item.HasImage(ImageType.Backdrop) ? item : null)
            ?? GetParentWithImage<BaseItem>(item, ImageType.Backdrop);

        if (itemWithImage is not null)
        {
            var tag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Backdrop);

            if (tag is not null)
            {
                hint.BackdropImageTag = tag;
                hint.BackdropImageItemId = itemWithImage.Id.ToString("N", CultureInfo.InvariantCulture);
            }
        }
    }

    private T? GetParentWithImage<T>(BaseItem item, ImageType type)
        where T : BaseItem
    {
        return item.GetParents().OfType<T>().FirstOrDefault(i => i.HasImage(type));
    }
}
