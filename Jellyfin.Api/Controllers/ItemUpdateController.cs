using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Item update controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public class ItemUpdateController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemUpdateController"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public ItemUpdateController(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            ILocalizationManager localizationManager,
            IServerConfigurationManager serverConfigurationManager)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _fileSystem = fileSystem;
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="request">The new item properties.</param>
        /// <response code="204">Item updated.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the item could not be found.</returns>
        [HttpPost("Items/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateItem([FromRoute, Required] Guid itemId, [FromBody, Required] BaseItemDto request)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var newLockData = request.LockData ?? false;
            var isLockedChanged = item.IsLocked != newLockData;

            var series = item as Series;
            var displayOrderChanged = series != null && !string.Equals(
                series.DisplayOrder ?? string.Empty,
                request.DisplayOrder ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            // Do this first so that metadata savers can pull the updates from the database.
            if (request.People != null)
            {
                _libraryManager.UpdatePeople(
                    item,
                    request.People.Select(x => new PersonInfo
                    {
                        Name = x.Name,
                        Role = x.Role,
                        Type = x.Type
                    }).ToList());
            }

            UpdateItem(request, item);

            item.OnMetadataChanged();

            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (isLockedChanged && item.IsFolder)
            {
                var folder = (Folder)item;

                foreach (var child in folder.GetRecursiveChildren())
                {
                    child.IsLocked = newLockData;
                    await child.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                }
            }

            if (displayOrderChanged)
            {
                _providerManager.QueueRefresh(
                    series!.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                        ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                        ReplaceAllMetadata = true
                    },
                    RefreshPriority.High);
            }

            return NoContent();
        }

        /// <summary>
        /// Gets metadata editor info for an item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <response code="200">Item metadata editor returned.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>An <see cref="OkResult"/> on success containing the metadata editor, or a <see cref="NotFoundResult"/> if the item could not be found.</returns>
        [HttpGet("Items/{itemId}/MetadataEditor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<MetadataEditorInfo> GetMetadataEditorInfo([FromRoute, Required] Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);

            var info = new MetadataEditorInfo
            {
                ParentalRatingOptions = _localizationManager.GetParentalRatings().ToArray(),
                ExternalIdInfos = _providerManager.GetExternalIdInfos(item).ToArray(),
                Countries = _localizationManager.GetCountries().ToArray(),
                Cultures = _localizationManager.GetCultures().ToArray()
            };

            if (!item.IsVirtualItem
                && !(item is ICollectionFolder)
                && !(item is UserView)
                && !(item is AggregateFolder)
                && !(item is LiveTvChannel)
                && !(item is IItemByName)
                && item.SourceType == SourceType.Library)
            {
                var inheritedContentType = _libraryManager.GetInheritedContentType(item);
                var configuredContentType = _libraryManager.GetConfiguredContentType(item);

                if (string.IsNullOrWhiteSpace(inheritedContentType) ||
                    !string.IsNullOrWhiteSpace(configuredContentType))
                {
                    info.ContentTypeOptions = GetContentTypeOptions(true).ToArray();
                    info.ContentType = configuredContentType;

                    if (string.IsNullOrWhiteSpace(inheritedContentType)
                        || string.Equals(inheritedContentType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                    {
                        info.ContentTypeOptions = info.ContentTypeOptions
                            .Where(i => string.IsNullOrWhiteSpace(i.Value)
                                        || string.Equals(i.Value, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Updates an item's content type.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="contentType">The content type of the item.</param>
        /// <response code="204">Item content type updated.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the item could not be found.</returns>
        [HttpPost("Items/{itemId}/ContentType")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateItemContentType([FromRoute, Required] Guid itemId, [FromQuery] string? contentType)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var path = item.ContainingFolderPath;

            var types = _serverConfigurationManager.Configuration.ContentTypes
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .Where(i => !string.Equals(i.Name, path, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                types.Add(new NameValuePair
                {
                    Name = path,
                    Value = contentType
                });
            }

            _serverConfigurationManager.Configuration.ContentTypes = types.ToArray();
            _serverConfigurationManager.SaveConfiguration();
            return NoContent();
        }

        private void UpdateItem(BaseItemDto request, BaseItem item)
        {
            item.Name = request.Name;
            item.ForcedSortName = request.ForcedSortName;

            item.OriginalTitle = string.IsNullOrWhiteSpace(request.OriginalTitle) ? null : request.OriginalTitle;

            item.CriticRating = request.CriticRating;

            item.CommunityRating = request.CommunityRating;
            item.IndexNumber = request.IndexNumber;
            item.ParentIndexNumber = request.ParentIndexNumber;
            item.Overview = request.Overview;
            item.Genres = request.Genres;

            if (item is Episode episode)
            {
                episode.AirsAfterSeasonNumber = request.AirsAfterSeasonNumber;
                episode.AirsBeforeEpisodeNumber = request.AirsBeforeEpisodeNumber;
                episode.AirsBeforeSeasonNumber = request.AirsBeforeSeasonNumber;
            }

            item.Tags = request.Tags;

            if (request.Taglines != null)
            {
                item.Tagline = request.Taglines.FirstOrDefault();
            }

            if (request.Studios != null)
            {
                item.Studios = request.Studios.Select(x => x.Name).ToArray();
            }

            if (request.DateCreated.HasValue)
            {
                item.DateCreated = NormalizeDateTime(request.DateCreated.Value);
            }

            item.EndDate = request.EndDate.HasValue ? NormalizeDateTime(request.EndDate.Value) : (DateTime?)null;
            item.PremiereDate = request.PremiereDate.HasValue ? NormalizeDateTime(request.PremiereDate.Value) : (DateTime?)null;
            item.ProductionYear = request.ProductionYear;
            item.OfficialRating = string.IsNullOrWhiteSpace(request.OfficialRating) ? null : request.OfficialRating;
            item.CustomRating = request.CustomRating;

            if (request.ProductionLocations != null)
            {
                item.ProductionLocations = request.ProductionLocations;
            }

            item.PreferredMetadataCountryCode = request.PreferredMetadataCountryCode;
            item.PreferredMetadataLanguage = request.PreferredMetadataLanguage;

            if (item is IHasDisplayOrder hasDisplayOrder)
            {
                hasDisplayOrder.DisplayOrder = request.DisplayOrder;
            }

            if (item is IHasAspectRatio hasAspectRatio)
            {
                hasAspectRatio.AspectRatio = request.AspectRatio;
            }

            item.IsLocked = request.LockData ?? false;

            if (request.LockedFields != null)
            {
                item.LockedFields = request.LockedFields;
            }

            // Only allow this for series. Runtimes for media comes from ffprobe.
            if (item is Series)
            {
                item.RunTimeTicks = request.RunTimeTicks;
            }

            foreach (var pair in request.ProviderIds.ToList())
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    request.ProviderIds.Remove(pair.Key);
                }
            }

            item.ProviderIds = request.ProviderIds;

            if (item is Video video)
            {
                video.Video3DFormat = request.Video3DFormat;
            }

            if (request.AlbumArtists != null)
            {
                if (item is IHasAlbumArtist hasAlbumArtists)
                {
                    hasAlbumArtists.AlbumArtists = request
                        .AlbumArtists
                        .Select(i => i.Name)
                        .ToArray();
                }
            }

            if (request.ArtistItems != null)
            {
                if (item is IHasArtist hasArtists)
                {
                    hasArtists.Artists = request
                        .ArtistItems
                        .Select(i => i.Name)
                        .ToArray();
                }
            }

            switch (item)
            {
                case Audio song:
                    song.Album = request.Album;
                    break;
                case MusicVideo musicVideo:
                    musicVideo.Album = request.Album;
                    break;
                case Series series:
                {
                    series.Status = GetSeriesStatus(request);

                    if (request.AirDays != null)
                    {
                        series.AirDays = request.AirDays;
                        series.AirTime = request.AirTime;
                    }

                    break;
                }
            }
        }

        private SeriesStatus? GetSeriesStatus(BaseItemDto item)
        {
            if (string.IsNullOrEmpty(item.Status))
            {
                return null;
            }

            return (SeriesStatus)Enum.Parse(typeof(SeriesStatus), item.Status, true);
        }

        private DateTime NormalizeDateTime(DateTime val)
        {
            return DateTime.SpecifyKind(val, DateTimeKind.Utc);
        }

        private List<NameValuePair> GetContentTypeOptions(bool isForItem)
        {
            var list = new List<NameValuePair>();

            if (isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "Inherit",
                    Value = string.Empty
                });
            }

            list.Add(new NameValuePair
            {
                Name = "Movies",
                Value = "movies"
            });
            list.Add(new NameValuePair
            {
                Name = "Music",
                Value = "music"
            });
            list.Add(new NameValuePair
            {
                Name = "Shows",
                Value = "tvshows"
            });

            if (!isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "Books",
                    Value = "books"
                });
            }

            list.Add(new NameValuePair
            {
                Name = "HomeVideos",
                Value = "homevideos"
            });
            list.Add(new NameValuePair
            {
                Name = "MusicVideos",
                Value = "musicvideos"
            });
            list.Add(new NameValuePair
            {
                Name = "Photos",
                Value = "photos"
            });

            if (!isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "MixedContent",
                    Value = string.Empty
                });
            }

            foreach (var val in list)
            {
                val.Name = _localizationManager.GetLocalizedString(val.Name);
            }

            return list;
        }
    }
}
