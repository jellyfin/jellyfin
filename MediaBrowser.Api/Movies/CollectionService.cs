using MediaBrowser.Controller.Collections;
using ServiceStack;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Movies
{
    [Route("/Collections", "POST")]
    [Api(Description = "Creates a new collection")]
    public class CreateCollection : IReturnVoid
    {
        [ApiMember(Name = "IsLocked", Description = "Whether or not to lock the new collection.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool IsLocked { get; set; }

        [ApiMember(Name = "Name", Description = "The name of the new collection.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "ParentId", Description = "Optional - create the collection within a specific folder", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public Guid? ParentId { get; set; }
    }

    [Route("/Collections/{Id}/Items", "POST")]
    [Api(Description = "Adds items to a collection")]
    public class AddToCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }
    }

    [Route("/Collections/{Id}/Items", "DELETE")]
    [Api(Description = "Removes items from a collection")]
    public class RemoveFromCollection : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    public class CollectionService : BaseApiService
    {
        private readonly ICollectionManager _collectionManager;

        public CollectionService(ICollectionManager collectionManager)
        {
            _collectionManager = collectionManager;
        }

        public void Post(CreateCollection request)
        {
            var task = _collectionManager.CreateCollection(new CollectionCreationOptions
            {
                IsLocked = request.IsLocked,
                Name = request.Name,
                ParentId = request.ParentId
            });

            Task.WaitAll(task);
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
