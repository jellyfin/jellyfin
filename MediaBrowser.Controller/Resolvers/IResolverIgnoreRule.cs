
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Provides a base "rule" that anyone can use to have paths ignored by the resolver
    /// </summary>
    public interface IResolverIgnoreRule
    {
        bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent);
    }
}
