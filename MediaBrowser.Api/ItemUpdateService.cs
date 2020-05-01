using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    [Route("/Items/{ItemId}", "POST", Summary = "Updates an item")]
    public class UpdateItem : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ItemId { get; set; }
    }

    [Route("/Items/{ItemId}/MetadataEditor", "GET", Summary = "Gets metadata editor info for an item")]
    public class GetMetadataEditorInfo : IReturn<MetadataEditorInfo>
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string ItemId { get; set; }
    }

    [Route("/Items/{ItemId}/ContentType", "POST", Summary = "Updates an item's content type")]
    public class UpdateItemContentType : IReturnVoid
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid ItemId { get; set; }

        [ApiMember(Name = "ContentType", Description = "The content type of the item", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ContentType { get; set; }
    }

    [Authenticated(Roles = "admin")]
    public class ItemUpdateService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IFileSystem _fileSystem;

        public ItemUpdateService(
            ILogger<ItemUpdateService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            ILocalizationManager localizationManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _fileSystem = fileSystem;
        }

        public object Get(GetMetadataEditorInfo request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);

            var info = new MetadataEditorInfo
            {
                ParentalRatingOptions = _localizationManager.GetParentalRatings().ToArray(),
                ExternalIdInfos = _providerManager.GetExternalIdInfos(item).ToArray(),
                Countries = _localizationManager.GetCountries().ToArray(),
                Cultures = _localizationManager.GetCultures().ToArray()
            };

            if (!item.IsVirtualItem && !(item is ICollectionFolder) && !(item is UserView) && !(item is AggregateFolder) && !(item is LiveTvChannel) && !(item is IItemByName) &&
                item.SourceType == SourceType.Library)
            {
                var inheritedContentType = _libraryManager.GetInheritedContentType(item);
                var configuredContentType = _libraryManager.GetConfiguredContentType(item);

                if (string.IsNullOrWhiteSpace(inheritedContentType) || !string.IsNullOrWhiteSpace(configuredContentType))
                {
                    info.ContentTypeOptions = GetContentTypeOptions(true).ToArray();
                    info.ContentType = configuredContentType;

                    if (string.IsNullOrWhiteSpace(inheritedContentType) || string.Equals(inheritedContentType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                    {
                        info.ContentTypeOptions = info.ContentTypeOptions
                            .Where(i => string.IsNullOrWhiteSpace(i.Value) || string.Equals(i.Value, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }
                }
            }

            return ToOptimizedResult(info);
        }

        public void Post(UpdateItemContentType request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);
            var path = item.ContainingFolderPath;

            var types = ServerConfigurationManager.Configuration.ContentTypes
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .Where(i => !string.Equals(i.Name, path, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                types.Add(new NameValuePair
                {
                    Name = path,
                    Value = request.ContentType
                });
            }

            ServerConfigurationManager.Configuration.ContentTypes = types.ToArray();
            ServerConfigurationManager.SaveConfiguration();
        }

        private List<NameValuePair> GetContentTypeOptions(bool isForItem)
        {
            var list = new List<NameValuePair>();

            if (isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "Inherit",
                    Value = ""
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
                    Value = ""
                });
            }

            foreach (var val in list)
            {
                val.Name = _localizationManager.GetLocalizedString(val.Name);
            }

            return list;
        }

        public void Post(UpdateItem request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);

            var newLockData = request.LockData ?? false;
            var isLockedChanged = item.IsLocked != newLockData;

            var series = item as Series;
            var displayOrderChanged = series != null && !string.Equals(series.DisplayOrder ?? string.Empty, request.DisplayOrder ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // Do this first so that metadata savers can pull the updates from the database.
            if (request.People != null)
            {
                _libraryManager.UpdatePeople(item, request.People.Select(x => new PersonInfo { Name = x.Name, Role = x.Role, Type = x.Type }).ToList());
            }

            UpdateItem(request, item);

            item.OnMetadataChanged();

            item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

            if (isLockedChanged && item.IsFolder)
            {
                var folder = (Folder)item;

                foreach (var child in folder.GetRecursiveChildren())
                {
                    child.IsLocked = newLockData;
                    child.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
                }
            }

            if (displayOrderChanged)
            {
                _providerManager.QueueRefresh(
                    series.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                        ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                        ReplaceAllMetadata = true
                    },
                    RefreshPriority.High);
            }
        }

        private DateTime NormalizeDateTime(DateTime val)
        {
            return DateTime.SpecifyKind(val, DateTimeKind.Utc);
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

            if (item is Audio song)
            {
                song.Album = request.Album;
            }

            if (item is MusicVideo musicVideo)
            {
                musicVideo.Album = request.Album;
            }

            if (item is Series series)
            {
                series.Status = GetSeriesStatus(request);

                if (request.AirDays != null)
                {
                    series.AirDays = request.AirDays;
                    series.AirTime = request.AirTime;
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
    }
}
