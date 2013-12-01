using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/LiveTv/Channels/{ChannelId}", "POST")]
    [Api(("Updates an item"))]
    public class UpdateChannel : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "ChannelId", Description = "The id of the channel", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ChannelId { get; set; }
    }

    [Route("/Items/{ItemId}", "POST")]
    [Api(("Updates an item"))]
    public class UpdateItem : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ItemId { get; set; }
    }

    [Route("/Artists/{ArtistName}", "POST")]
    [Api(("Updates an artist"))]
    public class UpdateArtist : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "ArtistName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ArtistName { get; set; }
    }

    [Route("/Studios/{StudioName}", "POST")]
    [Api(("Updates a studio"))]
    public class UpdateStudio : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "StudioName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string StudioName { get; set; }
    }

    [Route("/Persons/{PersonName}", "POST")]
    [Api(("Updates a person"))]
    public class UpdatePerson : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "PersonName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string PersonName { get; set; }
    }

    [Route("/MusicGenres/{GenreName}", "POST")]
    [Api(("Updates a music genre"))]
    public class UpdateMusicGenre : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "GenreName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string GenreName { get; set; }
    }

    [Route("/GameGenres/{GenreName}", "POST")]
    [Api(("Updates a game genre"))]
    public class UpdateGameGenre : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "GenreName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string GenreName { get; set; }
    }

    [Route("/Genres/{GenreName}", "POST")]
    [Api(("Updates a genre"))]
    public class UpdateGenre : BaseItemDto, IReturnVoid
    {
        [ApiMember(Name = "GenreName", Description = "The name of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string GenreName { get; set; }
    }

    public class ItemUpdateService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly ILiveTvManager _liveTv;

        public ItemUpdateService(ILibraryManager libraryManager, IDtoService dtoService, ILiveTvManager liveTv)
        {
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _liveTv = liveTv;
        }

        public void Post(UpdateItem request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        public void Post(UpdateChannel request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateItem request)
        {
            var item = _dtoService.GetItemByDtoId(request.ItemId);

            var newEnableInternetProviders = request.EnableInternetProviders ?? true;
            var dontFetchMetaChanged = item.DontFetchMeta != !newEnableInternetProviders;

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (dontFetchMetaChanged && item.IsFolder)
            {
                var folder = (Folder)item;

                foreach (var child in folder.RecursiveChildren.ToList())
                {
                    child.DontFetchMeta = !newEnableInternetProviders;
                    await _libraryManager.UpdateItem(child, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        public void Post(UpdatePerson request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdatePerson request)
        {
            var item = GetPerson(request.PersonName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task UpdateItem(UpdateChannel request)
        {
            var item = _liveTv.GetChannel(request.Id);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(UpdateArtist request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateArtist request)
        {
            var item = GetArtist(request.ArtistName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(UpdateStudio request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateStudio request)
        {
            var item = GetStudio(request.StudioName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(UpdateMusicGenre request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateMusicGenre request)
        {
            var item = GetMusicGenre(request.GenreName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(UpdateGameGenre request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateGameGenre request)
        {
            var item = GetGameGenre(request.GenreName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(UpdateGenre request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdateGenre request)
        {
            var item = GetGenre(request.GenreName, _libraryManager);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        private void UpdateItem(BaseItemDto request, BaseItem item)
        {
            if (item.LocationType == LocationType.Offline)
            {
                throw new InvalidOperationException(string.Format("{0} is currently offline.", item.Name));
            }

            item.Name = request.Name;

            // Only set the forced value if they changed it, or there's already one
            if (!string.Equals(item.SortName, request.SortName) || !string.IsNullOrEmpty(item.ForcedSortName))
            {
                item.ForcedSortName = request.SortName;
            }

            item.Budget = request.Budget;
            item.Revenue = request.Revenue;

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
            item.Tags = request.Tags;

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
                item.DateCreated = request.DateCreated.Value.ToUniversalTime();
            }

            item.EndDate = request.EndDate.HasValue ? request.EndDate.Value.ToUniversalTime() : (DateTime?)null;
            item.PremiereDate = request.PremiereDate.HasValue ? request.PremiereDate.Value.ToUniversalTime() : (DateTime?)null;
            item.ProductionYear = request.ProductionYear;
            item.ProductionLocations = request.ProductionLocations;
            item.Language = request.Language;
            item.OfficialRating = request.OfficialRating;
            item.CustomRating = request.CustomRating;

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                hasAspectRatio.AspectRatio = request.AspectRatio;
            }
            
            item.DontFetchMeta = !(request.EnableInternetProviders ?? true);
            if (request.EnableInternetProviders ?? true)
            {
                item.LockedFields = request.LockedFields;
            }
            else
            {
                item.LockedFields.Clear();
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

            var game = item as Game;

            if (game != null)
            {
                game.PlayersSupported = request.Players;
            }

            var song = item as Audio;

            if (song != null)
            {
                song.Album = request.Album;
                song.AlbumArtist = request.AlbumArtist;
                song.Artists = request.Artists.ToList();
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                musicVideo.Artist = request.Artists[0];
                musicVideo.Album = request.Album;
            }

            var series = item as Series;
            if (series != null)
            {
                series.Status = request.Status;
                series.AirDays = request.AirDays;
                series.AirTime = request.AirTime;
            }
        }

    }
}
