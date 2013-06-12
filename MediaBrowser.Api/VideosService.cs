using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Linq;

namespace MediaBrowser.Api
{
    [Route("/Videos/{Id}/AdditionalParts", "GET")]
    [Api(Description = "Gets additional parts for a video.")]
    public class GetAdditionalParts : IReturn<ItemsResult>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
    
    public class VideosService : BaseApiService
    {
        private readonly IItemRepository _itemRepo;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _userDataRepository;

        public VideosService(IItemRepository itemRepo, ILibraryManager libraryManager, IUserManager userManager, IUserDataRepository userDataRepository)
        {
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAdditionalParts request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : (Folder)_libraryManager.RootFolder)
                           : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var video = (Video)item;

            var items = _itemRepo.GetItems(video.AdditionalPartIds)
                         .OrderBy(i => i.SortName)
                         .Select(i => dtoBuilder.GetBaseItemDto(i, fields, user))
                         .Select(t => t.Result)
                         .ToArray();

            var result = new ItemsResult
            {
                Items = items,
                TotalRecordCount = items.Length
            };

            return ToOptimizedResult(result);
        }
    }
}
