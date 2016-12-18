using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System;
using System.IO;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class PlaylistResolver : FolderResolver<Playlist>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BoxSet.</returns>
        protected override Playlist Resolve(ItemResolveArgs args)
        {
            // It's a boxset if all of the following conditions are met:
            // Is a Directory
            // Contains [playlist] in the path
            if (args.IsDirectory)
            {
                var filename = Path.GetFileName(args.Path);

                if (string.IsNullOrEmpty(filename))
                {
                    return null;
                }

                if (filename.IndexOf("[playlist]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = ResolverHelper.StripBrackets(Path.GetFileName(args.Path))
                    };
                }
            }

            return null;
        }
    }
}
