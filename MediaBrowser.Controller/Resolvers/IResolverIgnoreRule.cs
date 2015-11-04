using CommonIO;
using MediaBrowser.Controller.Entities;

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
