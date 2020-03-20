#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// <see cref="IItemResolver"/> for <see cref="Playlist"/> library items.
    /// </summary>
    public class PlaylistResolver : FolderResolver<Playlist>
    {
        private string[] _musicPlaylistCollectionTypes = new string[] {
            string.Empty,
            CollectionType.Music
        };

        /// <inheritdoc/>
        protected override Playlist Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                // It's a boxset if the path is a directory with [playlist] in it's the name
                // TODO: Should this use Path.GetDirectoryName() instead?
                bool isBoxSet = Path.GetFileName(args.Path)
                    ?.Contains("[playlist]", StringComparison.OrdinalIgnoreCase)
                    ?? false;
                if (isBoxSet)
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = Path.GetFileName(args.Path).Replace("[playlist]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim()
                    };
                }

                // It's a directory-based playlist if the directory contains a playlist file
                var filePaths = Directory.EnumerateFiles(args.Path);
                if (filePaths.Any(f => f.EndsWith(PlaylistXmlSaver.DefaultPlaylistFilename, StringComparison.OrdinalIgnoreCase)))
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = Path.GetFileName(args.Path)
                    };
                }
            }

            // Check if this is a music playlist file
            // It should have the correct collection type and a supported file extension
            else if (_musicPlaylistCollectionTypes.Contains(args.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                var extension = Path.GetExtension(args.Path);
                if (Playlist.SupportedExtensions.Contains(extension ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    return new Playlist
                    {
                        Path = args.Path,
                        Name = Path.GetFileNameWithoutExtension(args.Path),
                        IsInMixedFolder = true
                    };
                }
            }

            return null;
        }
    }
}
