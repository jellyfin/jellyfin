using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
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
        private readonly IUserManager _userManager;

        public ItemUpdateService(ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public void Post(UpdateItem request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private Task UpdateItem(UpdateItem request)
        {
            var item = DtoBuilder.GetItemByClientId(request.ItemId, _userManager, _libraryManager);

            UpdateItem(request, item);

            return _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None);
        }

        public void Post(UpdatePerson request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private async Task UpdateItem(UpdatePerson request)
        {
            var item = await _libraryManager.GetPerson(request.PersonName).ConfigureAwait(false);

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
            var item = await _libraryManager.GetArtist(request.ArtistName).ConfigureAwait(false);

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
            var item = await _libraryManager.GetStudio(request.StudioName).ConfigureAwait(false);

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
            var item = await _libraryManager.GetMusicGenre(request.GenreName).ConfigureAwait(false);

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
            var item = await _libraryManager.GetGenre(request.GenreName).ConfigureAwait(false);

            UpdateItem(request, item);

            await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        private void UpdateItem(BaseItemDto request, BaseItem item)
        {
            item.Name = request.Name;

            // Only set the forced value if they changed it, or there's already one
            if (!string.Equals(item.SortName, request.SortName) || !string.IsNullOrEmpty(item.ForcedSortName))
            {
                item.ForcedSortName = request.SortName;
            }

            item.DisplayMediaType = request.DisplayMediaType;
            item.CommunityRating = request.CommunityRating;
            item.HomePageUrl = request.HomePageUrl;
            item.Budget = request.Budget;
            item.Revenue = request.Revenue;
            item.CriticRating = request.CriticRating;
            item.CriticRatingSummary = request.CriticRatingSummary;
            item.IndexNumber = request.IndexNumber;
            item.ParentIndexNumber = request.ParentIndexNumber;
            item.Overview = request.Overview;
            item.Genres = request.Genres;
            item.Tags = request.Tags;
            item.Studios = request.Studios.Select(x => x.Name).ToList();
            item.People = request.People.Select(x => new PersonInfo { Name = x.Name, Role = x.Role, Type = x.Type }).ToList();

            item.EndDate = request.EndDate != default(DateTime) ? request.EndDate : null;
            item.PremiereDate = request.PremiereDate != default(DateTime) ? request.PremiereDate : null;
            item.ProductionYear = request.ProductionYear;
            item.AspectRatio = request.AspectRatio;
            item.Language = request.Language;
            item.OfficialRating = request.OfficialRating;
            item.CustomRating = request.CustomRating;
            item.DontFetchMeta = !(request.EnableInternetProviders ?? true);
            if (request.EnableInternetProviders ?? true)
            {
                item.LockedFields = request.LockedFields;
            }
            else
            {
                item.LockedFields.Clear();
            }

            foreach (var pair in request.ProviderIds.ToList())
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    request.ProviderIds.Remove(pair.Key);
                }
            }

            item.ProviderIds = request.ProviderIds;

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
                song.Artist = request.Artists[0];
            }

            var musicAlbum = item as MusicAlbum;

            if (musicAlbum != null)
            {
                musicAlbum.MusicBrainzReleaseGroupId = request.GetProviderId("MusicBrainzReleaseGroupId");
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
