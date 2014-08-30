using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Collections;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public Guid? ParentId { get; set; }

        [ApiMember(Name = "Ids", Description = "Item Ids to add to the collection", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    [Route("/Collections/{Id}/Items", "POST", Summary = "Adds items to a collection")]
    public class AddToCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }
    }

    [Route("/Collections/{Id}/Items", "DELETE", Summary = "Removes items from a collection")]
    public class RemoveFromCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    [Authenticated]
    public class CollectionService : BaseApiService
    {
        private readonly ICollectionManager _collectionManager;
        private readonly IDtoService _dtoService;

        public CollectionService(ICollectionManager collectionManager, IDtoService dtoService)
        {
            _collectionManager = collectionManager;
            _dtoService = dtoService;
        }

        public async Task<object> Post(CreateCollection request)
        {
            var item = await _collectionManager.CreateCollection(new CollectionCreationOptions
            {
                IsLocked = request.IsLocked,
                Name = request.Name,
                ParentId = request.ParentId,
                ItemIdList = (request.Ids ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => new Guid(i)).ToList()

            }).ConfigureAwait(false);

            var dto = _dtoService.GetBaseItemDto(item, new List<ItemFields>());

            return ToOptimizedResult(new CollectionCreationResult
            {
                Id = dto.Id
            });
        }

        public void Post(AddToCollection request)
        {
            var task = _collectionManager.AddToCollection(request.Id, request.Ids.Split(',').Select(i => new Guid(i)));

            Task.WaitAll(task);
        }

        public void Delete(RemoveFromCollection request)
        {
            var task = _collectionManager.RemoveFromCollection(request.Id, request.Ids.Split(',').Select(i => new Guid(i)));

            Task.WaitAll(task);
        }
    }
}
