using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
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

    [Authenticated]
    public class ItemUpdateService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;

        public ItemUpdateService(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
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

            if (isLockedChanged && item.IsLocked)
            {
                item.IsUnidentified = false;
            }

            await item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (isLockedChanged && item.IsFolder)
            {
                var folder = (Folder)item;

                foreach (var child in folder.RecursiveChildren.ToList())
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

            var hasTags = item as IHasTags;
            if (hasTags != null)
            {
                hasTags.Tags = request.Tags;
            }

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

            var hasKeywords = item as IHasKeywords;
            if (hasKeywords != null)
            {
                hasKeywords.Keywords = request.Keywords;
            }

            if (request.Studios != null)
            {
                item.Studios = request.Studios.Select(x => x.Name).ToList();
            }

            if (request.People != null)
            {
                item.People = request.People.Select(x => new PersonInfo { Name = x.Name, Role = x.Role, Type = x.Type }).ToList();
            }

            if (request.DateCreated.HasValue)
            {
                item.DateCreated = NormalizeDateTime(request.DateCreated.Value);
            }

            item.EndDate = request.EndDate.HasValue ? NormalizeDateTime(request.EndDate.Value) : (DateTime?)null;
            item.PremiereDate = request.PremiereDate.HasValue ? NormalizeDateTime(request.PremiereDate.Value) : (DateTime?)null;
            item.ProductionYear = request.ProductionYear;
            item.OfficialRating = request.OfficialRating;
            item.CustomRating = request.CustomRating;

            SetProductionLocations(item, request);

            var hasLang = item as IHasPreferredMetadataLanguage;

            if (hasLang != null)
            {
                hasLang.PreferredMetadataCountryCode = request.PreferredMetadataCountryCode;
                hasLang.PreferredMetadataLanguage = request.PreferredMetadataLanguage;
            }

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

            item.IsLocked = (request.LockData ?? false);

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

            var song = item as Audio;

            if (song != null)
            {
                song.Album = request.Album;
                song.AlbumArtists = string.IsNullOrWhiteSpace(request.AlbumArtist) ? new List<string>() : new List<string> { request.AlbumArtist };
                song.Artists = request.Artists.ToList();
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                musicVideo.Artists = request.Artists.ToList();
                musicVideo.Album = request.Album;
            }

            var series = item as Series;
            if (series != null)
            {
                series.Status = request.Status;
                series.AirDays = request.AirDays;
                series.AirTime = request.AirTime;

                if (request.DisplaySpecialsWithSeasons.HasValue)
                {
                    series.DisplaySpecialsWithSeasons = request.DisplaySpecialsWithSeasons.Value;
                }
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
