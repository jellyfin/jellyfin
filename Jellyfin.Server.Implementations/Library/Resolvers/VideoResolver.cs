using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;

namespace Jellyfin.Server.Implementations.Library.Resolvers
{
    public class GenericVideoResolver<T> : BaseVideoResolver<T>
        where T : Video, new()
    {
        public GenericVideoResolver(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }
    }
}
