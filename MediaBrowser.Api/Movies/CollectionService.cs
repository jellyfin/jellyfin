using System;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Collections;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Movies
{
    [Route("/Collections", "POST", Summary = "Creates a new collection")]
    public class CreateCollection : IReturn<CollectionCreationResult>
    {
        [ApiMember(Name = "IsLocked", Description = "Whether or not to lock the new collection.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool IsLocked { get; set; }

        [ApiMember(Name = "Name", Description = "The name of the new collection.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "ParentId", Description = "Optional - create the collection within a specific folder", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ParentId { get; set; }

        [ApiMember(Name = "Ids", Description = "Item Ids to add to the collection", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    [Route("/Collections/{Id}/Items", "POST", Summary = "Adds items to a collection")]
    public class AddToCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Collections/{Id}/Items", "DELETE", Summary = "Removes items from a collection")]
    public class RemoveFromCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Authenticated]
    public class CollectionService : BaseApiService
    {
        private readonly ICollectionManager _collectionManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        public CollectionService(
            ILogger<CollectionService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ICollectionManager collectionManager,
            IDtoService dtoService,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _collectionManager = collectionManager;
            _dtoService = dtoService;
            _authContext = authContext;
        }

        public object Post(CreateCollection request)
        {
            var userId = _authContext.GetAuthorizationInfo(Request).UserId;

            var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? (Guid?)null : new Guid(request.ParentId);

            var item = _collectionManager.CreateCollection(new CollectionCreationOptions
            {
                IsLocked = request.IsLocked,
                Name = request.Name,
                ParentId = parentId,
                ItemIdList = SplitValue(request.Ids, ','),
                UserIds = new[] { userId }

            });

            var dtoOptions = GetDtoOptions(_authContext, request);

            var dto = _dtoService.GetBaseItemDto(item, dtoOptions);

            return new CollectionCreationResult
            {
                Id = dto.Id
            };
        }

        public void Post(AddToCollection request)
        {
            _collectionManager.AddToCollection(new Guid(request.Id), SplitValue(request.Ids, ','));
        }

        public void Delete(RemoveFromCollection request)
        {
            _collectionManager.RemoveFromCollection(new Guid(request.Id), SplitValue(request.Ids, ','));
        }
    }
}
