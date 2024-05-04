#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using PlaylistsNET.Content;

namespace MediaBrowser.Providers.Playlists
{
    public class PlaylistItemsProvider : ICustomMetadataProvider<Playlist>,
        IHasOrder,
        IForcedProvider,
        IPreRefreshProvider,
        IHasItemChangeMonitor
    {
        private readonly ILogger<PlaylistItemsProvider> _logger;

        public PlaylistItemsProvider(ILogger<PlaylistItemsProvider> logger)
        {
            _logger = logger;
        }

        public string Name => "Playlist Reader";

        // Run last
        public int Order => 100;

        public Task<ItemUpdateType> FetchAsync(Playlist item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var path = item.Path;
            if (!Playlist.IsPlaylistFile(path))
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            var extension = Path.GetExtension(path);
            if (!Playlist.SupportedExtensions.Contains(extension ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            var items = GetItems(path, extension).ToArray();

            item.LinkedChildren = items;

            return Task.FromResult(ItemUpdateType.None);
        }

        private IEnumerable<LinkedChild> GetItems(string path, string extension)
        {
            using (var stream = File.OpenRead(path))
            {
                if (string.Equals(".wpl", extension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetWplItems(stream, path);
                }

                if (string.Equals(".zpl", extension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetZplItems(stream, path);
                }

                if (string.Equals(".m3u", extension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetM3uItems(stream, path);
                }

                if (string.Equals(".m3u8", extension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetM3u8Items(stream, path);
                }

                if (string.Equals(".pls", extension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetPlsItems(stream, path);
                }
            }

            return Enumerable.Empty<LinkedChild>();
        }

        private IEnumerable<LinkedChild> GetPlsItems(Stream stream, string path)
        {
            var content = new PlsContent();
            var playlist = content.GetFromStream(stream);

            return playlist.PlaylistEntries.Select(i => new LinkedChild
            {
                Path = GetPlaylistItemPath(i.Path, path),
                Type = LinkedChildType.Manual
            });
        }

        private IEnumerable<LinkedChild> GetM3u8Items(Stream stream, string path)
        {
            var content = new M3uContent();
            var playlist = content.GetFromStream(stream);

            return playlist.PlaylistEntries.Select(i => new LinkedChild
            {
                Path = GetPlaylistItemPath(i.Path, path),
                Type = LinkedChildType.Manual
            });
        }

        private IEnumerable<LinkedChild> GetM3uItems(Stream stream, string path)
        {
            var content = new M3uContent();
            var playlist = content.GetFromStream(stream);

            return playlist.PlaylistEntries.Select(i => new LinkedChild
            {
                Path = GetPlaylistItemPath(i.Path, path),
                Type = LinkedChildType.Manual
            });
        }

        private IEnumerable<LinkedChild> GetZplItems(Stream stream, string path)
        {
            var content = new ZplContent();
            var playlist = content.GetFromStream(stream);

            return playlist.PlaylistEntries.Select(i => new LinkedChild
            {
                Path = GetPlaylistItemPath(i.Path, path),
                Type = LinkedChildType.Manual
            });
        }

        private IEnumerable<LinkedChild> GetWplItems(Stream stream, string path)
        {
            var content = new WplContent();
            var playlist = content.GetFromStream(stream);

            return playlist.PlaylistEntries.Select(i => new LinkedChild
            {
                Path = GetPlaylistItemPath(i.Path, path),
                Type = LinkedChildType.Manual
            });
        }

        private string GetPlaylistItemPath(string itemPath, string containingPlaylistFolder)
        {
            if (!File.Exists(itemPath))
            {
                var path = Path.Combine(Path.GetDirectoryName(containingPlaylistFolder), itemPath);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return itemPath;
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var path = item.Path;

            if (!string.IsNullOrWhiteSpace(path) && item.IsFileProtocol)
            {
                var file = directoryService.GetFile(path);
                if (file is not null && file.LastWriteTimeUtc != item.DateModified)
                {
                    _logger.LogDebug("Refreshing {Path} due to date modified timestamp change.", path);
                    return true;
                }
            }

            return false;
        }
    }
}
