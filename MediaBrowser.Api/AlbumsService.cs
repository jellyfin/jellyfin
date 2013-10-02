using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    [Route("/Albums/{Id}/Similar", "GET")]
    [Api(Description = "Finds albums similar to a given album.")]
    public class GetSimilarAlbums : BaseGetSimilarItemsFromItem
    {
    }

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

        public AlbumsService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarAlbums request)
        {
            var result = SimilarItemsHelper.GetSimilarItemsResult(_userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,
                request, item => item is MusicAlbum,
                GetAlbumSimilarityScore);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the album similarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        private int GetAlbumSimilarityScore(BaseItem item1, BaseItem item2)
        {
            var points = SimilarItemsHelper.GetSimiliarityScore(item1, item2);

            var album1 = (MusicAlbum)item1;
            var album2 = (MusicAlbum)item2;

            var artists1 = album1.GetRecursiveChildren()
                .OfType<Audio>()
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var artists2 = album2.GetRecursiveChildren()
                .OfType<Audio>()
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            return points + artists1.Where(artists2.ContainsKey).Sum(i => 5);
        }
    }
}
