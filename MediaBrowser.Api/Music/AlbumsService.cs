using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Music
{
    [Route("/Albums/{Id}/Similar", "GET", Summary = "Finds albums similar to a given album.")]
    public class GetSimilarAlbums : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Artists/{Id}/Similar", "GET", Summary = "Finds albums similar to a given album.")]
    public class GetSimilarArtists : BaseGetSimilarItemsFromItem
    {
    }

    [Authenticated]
    public class AlbumsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataManager _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        public AlbumsService(
            ILogger<AlbumsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            IUserDataManager userDataRepository,
            ILibraryManager libraryManager,
            IItemRepository itemRepo,
            IDtoService dtoService,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
            _authContext = authContext;
        }

        public object Get(GetSimilarArtists request)
        {
            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = SimilarItemsHelper.GetSimilarItemsResult(dtoOptions, _userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,
                request, new[] { typeof(MusicArtist) },
                SimilarItemsHelper.GetSimiliarityScore);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarAlbums request)
        {
            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = SimilarItemsHelper.GetSimilarItemsResult(dtoOptions, _userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,
                request, new[] { typeof(MusicAlbum) },
                GetAlbumSimilarityScore);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the album similarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item1People">The item1 people.</param>
        /// <param name="allPeople">All people.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        private int GetAlbumSimilarityScore(BaseItem item1, List<PersonInfo> item1People, List<PersonInfo> allPeople, BaseItem item2)
        {
            var points = SimilarItemsHelper.GetSimiliarityScore(item1, item1People, allPeople, item2);

            var album1 = (MusicAlbum)item1;
            var album2 = (MusicAlbum)item2;

            var artists1 = album1
                .GetAllArtists()
                .DistinctNames()
                .ToList();

            var artists2 = new HashSet<string>(
                album2.GetAllArtists().DistinctNames(),
                StringComparer.OrdinalIgnoreCase);

            return points + artists1.Where(artists2.Contains).Sum(i => 5);
        }
    }
}
