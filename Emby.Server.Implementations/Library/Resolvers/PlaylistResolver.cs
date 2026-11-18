#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.LocalMetadata.Savers;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// <see cref="IItemResolver"/> for <see cref="Playlist"/> library items.
    /// </summary>
    public class PlaylistResolver : GenericFolderResolver<Playlist>
    {
        private readonly CollectionType?[] _musicPlaylistCollectionTypes =
        [
            null,
            CollectionType.music
        ];

        /// <inheritdoc/>
        protected override Playlist Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                // It's a playlist if the path is a directory with [playlist] in its name
                var filename = Path.GetFileName(Path.TrimEndingDirectorySeparator(args.Path));
                if (string.IsNullOrEmpty(filename))
                {
                    return null;
                }

                if (filename.Contains("[playlist]", StringComparison.OrdinalIgnoreCase))
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = filename.Replace("[playlist]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(),
                        OpenAccess = true
                    };
                }

                // Anything directly inside the internal playlists folder is a playlist, even when its
                // playlist.xml is missing: failing to resolve here makes the library scan treat the
                // playlist as deleted from disk and remove it, taking its items with it.
                if (args.Parent is PlaylistsFolder)
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = filename,
                        OpenAccess = true
                    };
                }

                // It's a directory-based playlist if the directory contains a playlist file
                IEnumerable<string> filePaths;
                try
                {
                    filePaths = Directory.EnumerateFiles(args.Path, "*", new EnumerationOptions { IgnoreInaccessible = true });
                }
                catch (IOException)
                {
                    return null;
                }

                if (filePaths.Any(f => f.EndsWith(PlaylistXmlSaver.DefaultPlaylistFilename, StringComparison.OrdinalIgnoreCase)))
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = filename,
                        OpenAccess = true
                    };
                }
            }

            // Check if this is a music playlist file
            // It should have the correct collection type and a supported file extension
            else if (_musicPlaylistCollectionTypes.Contains(args.CollectionType))
            {
                var extension = Path.GetExtension(args.Path.AsSpan());
                if (Playlist.SupportedExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = Path.GetFileNameWithoutExtension(args.Path),
                        IsInMixedFolder = true,
                        PlaylistMediaType = MediaType.Audio,
                        OpenAccess = true
                    };
                }
            }

            return null;
        }
    }
}
