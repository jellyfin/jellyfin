#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class PlaylistResolver : FolderResolver<Playlist>
    {
        private string[] SupportedCollectionTypes = new string[] {

            string.Empty,
            CollectionType.Music
        };

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
            else
            {
                if (SupportedCollectionTypes.Contains(args.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
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
            }

            return null;
        }
    }
}
