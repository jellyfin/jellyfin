#nullable disable

using System;
using System.IO;
using System.Linq;
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

                // It's a directory-based playlist if the directory contains a playlist file
                var filePaths = Directory.EnumerateFiles(args.Path, "*", new EnumerationOptions { IgnoreInaccessible = true });
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
