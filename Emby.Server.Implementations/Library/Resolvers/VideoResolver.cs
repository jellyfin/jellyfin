#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Library.Resolvers
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
