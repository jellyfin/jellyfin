using Jellyfin.Controller.Entities;
using Jellyfin.Model.IO;

namespace Jellyfin.Controller.Resolvers
{
    /// <summary>
    /// Provides a base "rule" that anyone can use to have paths ignored by the resolver
    /// </summary>
    public interface IResolverIgnoreRule
    {
        bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent);
    }
}
