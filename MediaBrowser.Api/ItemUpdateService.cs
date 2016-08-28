using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public string ItemId { get; set; }

        [ApiMember(Name = "ContentType", Description = "The content type of the item", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ContentType { get; set; }
    }

    [Authenticated(Roles = "admin")]
    public class ItemUpdateService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IServerConfigurationManager _config;

        public ItemUpdateService(ILibraryManager libraryManager, IProviderManager providerManager, ILocalizationManager localizationManager, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _localizationManager = localizationManager;
            _config = config;
        }

        public object Get(GetMetadataEditorInfo request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);

            var info = new MetadataEditorInfo
            {
                ParentalRatingOptions = _localizationManager.GetParentalRatings().ToList(),
                ExternalIdInfos = _providerManager.GetExternalIdInfos(item).ToList(),
                Countries = _localizationManager.GetCountries().ToList(),
                Cultures = _localizationManager.GetCultures().ToList()
            };

            if (!item.IsVirtualItem && !(item is ICollectionFolder) && !(item is UserView) && !(item is AggregateFolder) && !(item is LiveTvChannel) && !(item is IItemByName))
            {
                var inheritedContentType = _libraryManager.GetInheritedContentType(item);
                var configuredContentType = _libraryManager.GetConfiguredContentType(item);

                if (string.IsNullOrWhiteSpace(inheritedContentType) || string.Equals(inheritedContentType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(configuredContentType))
                {
                    info.ContentTypeOptions = GetContentTypeOptions(true);
                    info.ContentType = configuredContentType;

                    if (string.Equals(inheritedContentType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                    {
                        info.ContentTypeOptions = info.ContentTypeOptions
                            .Where(i => string.IsNullOrWhiteSpace(i.Value) || string.Equals(i.Value, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }
            }

            return ToOptimizedResult(info);
        }

        public void Post(UpdateItemContentType request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);
            var path = item.ContainingFolderPath;

            var types = _config.Configuration.ContentTypes
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

            _config.Configuration.ContentTypes = types.ToArray();
            _config.SaveConfiguration();
        }

        private List<NameValuePair> GetContentTypeOptions(bool isForItem)
        {
            var list = new List<NameValuePair>();

            if (isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "FolderTypeInherit",
                    Value = ""
                });
            }

            list.Add(new NameValuePair
            {
                Name = "FolderTypeMovies",
                Value = "movies"
            });
            list.Add(new NameValuePair
            {
                Name = "FolderTypeMusic",
                Value = "music"
            });
            list.Add(new NameValuePair
            {
                Name = "FolderTypeTvShows",
                Value = "tvshows"
            });

            if (!isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "FolderTypeBooks",
                    Value = "books"
                });
                list.Add(new NameValuePair
                {
                    Name = "FolderTypeGames",
                    Value = "games"
                });
            }

            list.Add(new NameValuePair
            {
                Name = "FolderTypeHomeVideos",
                Value = "homevideos"
            });
            list.Add(new NameValuePair
            {
                Name = "FolderTypeMusicVideos",
                Value = "musicvideos"
            });
            list.Add(new NameValuePair
            {
                Name = "FolderTypePhotos",
                Value = "photos"
            });

            if (!isForItem)
            {
                list.Add(new NameValuePair
                {
                    Name = "FolderTypeMixed",
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
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateItem request)
        {
            var item = _libraryManager.GetItemById(request.ItemId);

            var newLockData = request.LockData ?? false;
            var isLockedChanged = item.IsLocked != newLockData;

            UpdateItem(request, item);

            await item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (request.People != null)
            {
                await _libraryManager.UpdatePeople(item, request.People.Select(x => new PersonInfo { Name = x.Name, Role = x.Role, Type = x.Type }).ToList());
            }

            if (isLockedChanged && item.IsFolder)
            {
                var folder = (Folder)item;

                foreach (var child in folder.GetRecursiveChildren())
                {
                    child.IsLocked = newLockData;
                    await child.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                }
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

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                hasBudget.Budget = request.Budget;
                hasBudget.Revenue = request.Revenue;
            }

            var hasOriginalTitle = item as IHasOriginalTitle;
            if (hasOriginalTitle != null)
            {
                hasOriginalTitle.OriginalTitle = hasOriginalTitle.OriginalTitle;
            }

            var hasCriticRating = item as IHasCriticRating;
            if (hasCriticRating != null)
            {
                hasCriticRating.CriticRating = request.CriticRating;
                hasCriticRating.CriticRatingSummary = request.CriticRatingSummary;
            }

            item.DisplayMediaType = request.DisplayMediaType;
            item.CommunityRating = request.CommunityRating;
            item.VoteCount = request.VoteCount;
            item.HomePageUrl = request.HomePageUrl;
            item.IndexNumber = request.IndexNumber;
            item.ParentIndexNumber = request.ParentIndexNumber;
            item.Overview = request.Overview;
            item.Genres = request.Genres;

            var episode = item as Episode;
            if (episode != null)
            {
                episode.DvdSeasonNumber = request.DvdSeasonNumber;
                episode.DvdEpisodeNumber = request.DvdEpisodeNumber;
                episode.AirsAfterSeasonNumber = request.AirsAfterSeasonNumber;
                episode.AirsBeforeEpisodeNumber = request.AirsBeforeEpisodeNumber;
                episode.AirsBeforeSeasonNumber = request.AirsBeforeSeasonNumber;
                episode.AbsoluteEpisodeNumber = request.AbsoluteEpisodeNumber;
            }

            item.Tags = request.Tags;

            var hasTaglines = item as IHasTaglines;
            if (hasTaglines != null)
            {
                hasTaglines.Taglines = request.Taglines;
            }

            var hasShortOverview = item as IHasShortOverview;
            if (hasShortOverview != null)
            {
                hasShortOverview.ShortOverview = request.ShortOverview;
            }

            item.Keywords = request.Keywords;

            if (request.Studios != null)
            {
                item.Studios = request.Studios.Select(x => x.Name).ToList();
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

            SetProductionLocations(item, request);

            item.PreferredMetadataCountryCode = request.PreferredMetadataCountryCode;
            item.PreferredMetadataLanguage = request.PreferredMetadataLanguage;

            var hasDisplayOrder = item as IHasDisplayOrder;
            if (hasDisplayOrder != null)
            {
                hasDisplayOrder.DisplayOrder = request.DisplayOrder;
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
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

            var video = item as Video;
            if (video != null)
            {
                video.Video3DFormat = request.Video3DFormat;
            }

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null)
            {
                hasMetascore.Metascore = request.Metascore;
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null)
            {
                hasAwards.AwardSummary = request.AwardSummary;
            }

            var game = item as Game;

            if (game != null)
            {
                game.PlayersSupported = request.Players;
            }

            if (request.AlbumArtists != null)
            {
                var hasAlbumArtists = item as IHasAlbumArtist;
                if (hasAlbumArtists != null)
                {
                    hasAlbumArtists.AlbumArtists = request
                        .AlbumArtists
                        .Select(i => i.Name)
                        .ToList();
                }
            }

            if (request.ArtistItems != null)
            {
                var hasArtists = item as IHasArtist;
                if (hasArtists != null)
                {
                    hasArtists.Artists = request
                        .ArtistItems
                        .Select(i => i.Name)
                        .ToList();
                }
            }

            var song = item as Audio;
            if (song != null)
            {
                song.Album = request.Album;
            }

            var musicVideo = item as MusicVideo;
            if (musicVideo != null)
            {
                musicVideo.Album = request.Album;
            }

            var series = item as Series;
            if (series != null)
            {
                series.Status = request.SeriesStatus;
                series.AirDays = request.AirDays;
                series.AirTime = request.AirTime;
            }
        }

        private void SetProductionLocations(BaseItem item, BaseItemDto request)
        {
            var hasProductionLocations = item as IHasProductionLocations;

            if (hasProductionLocations != null)
            {
                hasProductionLocations.ProductionLocations = request.ProductionLocations;
            }

            var person = item as Person;
            if (person != null)
            {
                person.PlaceOfBirth = request.ProductionLocations == null
                    ? null
                    : request.ProductionLocations.FirstOrDefault();
            }
        }
    }
}
