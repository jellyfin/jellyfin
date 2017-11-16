using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class GenericVideoResolver<T> : BaseVideoResolver<T>
        where T : Video, new ()
    {
        public GenericVideoResolver(ILibraryManager libraryManager, IFileSystem fileSystem) : base(libraryManager, fileSystem)
        {
        }
    }
}
