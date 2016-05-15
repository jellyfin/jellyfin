using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Api
{
    [Route("/Videos/{Id}/AdditionalParts", "GET", Summary = "Gets additional parts for a video.")]
    [Authenticated]
    public class GetAdditionalParts : IReturn<ItemsResult>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Videos/{Id}/AlternateSources", "DELETE", Summary = "Removes alternate video sources.")]
    [Authenticated(Roles = "Admin")]
    public class DeleteAlternateSources : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Videos/MergeVersions", "POST", Summary = "Merges videos into a single record")]
    [Authenticated(Roles = "Admin")]
    public class MergeVersions : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id list. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    public class VideosService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IFileSystem _fileSystem;
        private readonly IItemRepository _itemRepo;
        private readonly IServerConfigurationManager _config;

        public VideosService(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService, IItemRepository itemRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAdditionalParts request)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (!string.IsNullOrWhiteSpace(request.UserId)
                                  ? user.RootFolder
                                  : _libraryManager.RootFolder)
                           : _libraryManager.GetItemById(request.Id);

            var dtoOptions = GetDtoOptions(request);

            var video = (Video)item;

            var items = video.GetAdditionalParts()
                         .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, video))
                         .ToArray();

            var result = new ItemsResult
            {
                Items = items,
                TotalRecordCount = items.Length
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public void Delete(DeleteAlternateSources request)
        {
            var task = DeleteAsync(request);

            Task.WaitAll(task);
        }

        public async Task DeleteAsync(DeleteAlternateSources request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            foreach (var link in video.GetLinkedAlternateVersions())
            {
                link.PrimaryVersionId = null;

                await link.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }

            video.LinkedAlternateVersions.Clear();
            await video.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(MergeVersions request)
        {
            var task = PostAsync(request);

            Task.WaitAll(task);
        }

        public async Task PostAsync(MergeVersions request)
        {
            var items = request.Ids.Split(',')
                .Select(i => new Guid(i))
                .Select(i => _libraryManager.GetItemById(i))
                .OfType<Video>()
                .ToList();

            if (items.Count < 2)
            {
                throw new ArgumentException("Please supply at least two videos to merge.");
            }

            var videosWithVersions = items.Where(i => i.MediaSourceCount > 1)
                .ToList();

            if (videosWithVersions.Count > 1)
            {
                throw new ArgumentException("Videos with sub-versions cannot be merged.");
            }

            var primaryVersion = videosWithVersions.FirstOrDefault();

            if (primaryVersion == null)
            {
                primaryVersion = items.OrderBy(i =>
                {
                    if (i.Video3DFormat.HasValue)
                    {
                        return 1;
                    }

                    if (i.VideoType != Model.Entities.VideoType.VideoFile)
                    {
                        return 1;
                    }

                    return 0;
                })
                    .ThenByDescending(i =>
                    {
                        var stream = i.GetDefaultVideoStream();

                        return stream == null || stream.Width == null ? 0 : stream.Width.Value;

                    }).First();
            }

            foreach (var item in items.Where(i => i.Id != primaryVersion.Id))
            {
                item.PrimaryVersionId = primaryVersion.Id.ToString("N");

                await item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

                primaryVersion.LinkedAlternateVersions.Add(new LinkedChild
                {
                    Path = item.Path,
                    ItemId = item.Id
                });
            }

            await primaryVersion.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
