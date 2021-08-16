#nullable disable

using System;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class BoxSetResolver.
    /// </summary>
    public class BoxSetResolver : FolderResolver<BoxSet>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BoxSet.</returns>
        protected override BoxSet Resolve(ItemResolveArgs args)
        {
            // It's a boxset if all of the following conditions are met:
            // Is a Directory
            // Contains [boxset] in the path
            if (args.IsDirectory)
            {
                var filename = Path.GetFileName(args.Path);

                if (string.IsNullOrEmpty(filename))
                {
                    return null;
                }

                if (filename.IndexOf("[boxset]", StringComparison.OrdinalIgnoreCase) != -1 || args.ContainsFileSystemEntryByName("collection.xml"))
                {
                    return new BoxSet
                    {
                        Path = args.Path,
                        Name = Path.GetFileName(args.Path).Replace("[boxset]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim()
                    };
                }
            }

            return null;
        }
    }
}
