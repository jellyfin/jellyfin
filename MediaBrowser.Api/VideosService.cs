using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    [Route("/Videos/{Id}/AdditionalParts", "GET", Summary = "Gets additional parts for a video.")]
    [Authenticated]
    public class GetAdditionalParts : IReturn<QueryResult<BaseItemDto>>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

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
        private readonly IAuthorizationContext _authContext;

        public VideosService(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService, IItemRepository itemRepo, IFileSystem fileSystem, IServerConfigurationManager config, IAuthorizationContext authContext)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
            _config = config;
            _authContext = authContext;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAdditionalParts request)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (!request.UserId.Equals(Guid.Empty)
                                  ? _libraryManager.GetUserRootFolder()
                                  : _libraryManager.RootFolder)
                           : _libraryManager.GetItemById(request.Id);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var video = item as Video;
            BaseItemDto[] items;
            if (video != null)
            {
                items = video.GetAdditionalParts()
                    .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, video))
                    .ToArray();
            }
            else
            {
                items = new BaseItemDto[] { };
            }

            var result = new QueryResult<BaseItemDto>
            {
                Items = items,
                TotalRecordCount = items.Length
            };

            return ToOptimizedResult(result);
        }

        public void Delete(DeleteAlternateSources request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            foreach (var link in video.GetLinkedAlternateVersions())
            {
                link.SetPrimaryVersionId(null);
                link.LinkedAlternateVersions = Array.Empty<LinkedChild>();

                link.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
            }

            video.LinkedAlternateVersions = Array.Empty<LinkedChild>();
            video.SetPrimaryVersionId(null);
            video.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
        }

        public void Post(MergeVersions request)
        {
            var items = request.Ids.Split(',')
                .Select(i => _libraryManager.GetItemById(i))
                .OfType<Video>()
                .ToList();

            if (items.Count < 2)
            {
                throw new ArgumentException("Please supply at least two videos to merge.");
            }

            var videosWithVersions = items.Where(i => i.MediaSourceCount > 1)
                .ToList();

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

            var list = primaryVersion.LinkedAlternateVersions.ToList();

            foreach (var item in items.Where(i => i.Id != primaryVersion.Id))
            {
                item.SetPrimaryVersionId(primaryVersion.Id.ToString("N"));

                item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

                list.Add(new LinkedChild
                {
                    Path = item.Path,
                    ItemId = item.Id
                });

                foreach (var linkedItem in item.LinkedAlternateVersions)
                {
                    if (!list.Any(i => string.Equals(i.Path, linkedItem.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(linkedItem);
                    }
                }

                if (item.LinkedAlternateVersions.Length > 0)
                {
                    item.LinkedAlternateVersions = Array.Empty<LinkedChild>();
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
                }
            }

            primaryVersion.LinkedAlternateVersions = list.ToArray();
            primaryVersion.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
        }
    }
}
