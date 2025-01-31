#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using PlaylistsNET.Content;

namespace MediaBrowser.Providers.Playlists;

/// <summary>
/// Local playlist provider.
/// </summary>
public class PlaylistItemsProvider : ILocalMetadataProvider<Playlist>,
    IHasOrder,
    IForcedProvider,
    IHasItemChangeMonitor
{
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<PlaylistItemsProvider> _logger;
    private readonly CollectionType[] _ignoredCollections = [CollectionType.livetv, CollectionType.boxsets, CollectionType.playlists];

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistItemsProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{PlaylistItemsProvider}"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public PlaylistItemsProvider(ILogger<PlaylistItemsProvider> logger, ILibraryManager libraryManager, IFileSystem fileSystem)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public string Name => "Playlist Item Provider";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public Task<MetadataResult<Playlist>> GetMetadata(
        ItemInfo info,
        IDirectoryService directoryService,
        CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Playlist>()
        {
            Item = new Playlist
            {
                Path = info.Path
            }
        };
        Fetch(result);

        return Task.FromResult(result);
    }

    private void Fetch(MetadataResult<Playlist> result)
    {
        var item = result.Item;
        var path = item.Path;
        if (!Playlist.IsPlaylistFile(path))
        {
            return;
        }

        var extension = Path.GetExtension(path);
        if (!Playlist.SupportedExtensions.Contains(extension ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var items = GetItems(path, extension).ToArray();
        if (items.Length > 0)
        {
            result.HasMetadata = true;
            item.LinkedChildren = items;
        }

        return;
    }

    private IEnumerable<LinkedChild> GetItems(string path, string extension)
    {
        var libraryRoots = _libraryManager.GetUserRootFolder().Children
                            .OfType<CollectionFolder>()
                            .Where(f => f.CollectionType.HasValue && !_ignoredCollections.Contains(f.CollectionType.Value))
                            .SelectMany(f => f.PhysicalLocations)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

        using (var stream = File.OpenRead(path))
        {
            if (string.Equals(".wpl", extension, StringComparison.OrdinalIgnoreCase))
            {
                return GetWplItems(stream, path, libraryRoots);
            }

            if (string.Equals(".zpl", extension, StringComparison.OrdinalIgnoreCase))
            {
                return GetZplItems(stream, path, libraryRoots);
            }

            if (string.Equals(".m3u", extension, StringComparison.OrdinalIgnoreCase))
            {
                return GetM3uItems(stream, path, libraryRoots);
            }

            if (string.Equals(".m3u8", extension, StringComparison.OrdinalIgnoreCase))
            {
                return GetM3uItems(stream, path, libraryRoots);
            }

            if (string.Equals(".pls", extension, StringComparison.OrdinalIgnoreCase))
            {
                return GetPlsItems(stream, path, libraryRoots);
            }
        }

        return Enumerable.Empty<LinkedChild>();
    }

    private IEnumerable<LinkedChild> GetPlsItems(Stream stream, string playlistPath, List<string> libraryRoots)
    {
        var content = new PlsContent();
        var playlist = content.GetFromStream(stream);

        return playlist.PlaylistEntries
                .Select(i => GetLinkedChild(i.Path, playlistPath, libraryRoots))
                .Where(i => i is not null);
    }

    private IEnumerable<LinkedChild> GetM3uItems(Stream stream, string playlistPath, List<string> libraryRoots)
    {
        var content = new M3uContent();
        var playlist = content.GetFromStream(stream);

        return playlist.PlaylistEntries
                .Select(i => GetLinkedChild(i.Path, playlistPath, libraryRoots))
                .Where(i => i is not null);
    }

    private IEnumerable<LinkedChild> GetZplItems(Stream stream, string playlistPath, List<string> libraryRoots)
    {
        var content = new ZplContent();
        var playlist = content.GetFromStream(stream);

        return playlist.PlaylistEntries
                .Select(i => GetLinkedChild(i.Path, playlistPath, libraryRoots))
                .Where(i => i is not null);
    }

    private IEnumerable<LinkedChild> GetWplItems(Stream stream, string playlistPath, List<string> libraryRoots)
    {
        var content = new WplContent();
        var playlist = content.GetFromStream(stream);

        return playlist.PlaylistEntries
                .Select(i => GetLinkedChild(i.Path, playlistPath, libraryRoots))
                .Where(i => i is not null);
    }

    private LinkedChild GetLinkedChild(string itemPath, string playlistPath, List<string> libraryRoots)
    {
        if (TryGetPlaylistItemPath(itemPath, playlistPath, libraryRoots, out var parsedPath))
        {
            return new LinkedChild
            {
                Path = parsedPath,
                Type = LinkedChildType.Manual
            };
        }

        return null;
    }

    private bool TryGetPlaylistItemPath(string itemPath, string playlistPath, List<string> libraryPaths, out string path)
    {
        path = null;
        string pathToCheck = _fileSystem.MakeAbsolutePath(Path.GetDirectoryName(playlistPath), itemPath);
        if (!File.Exists(pathToCheck))
        {
            return false;
        }

        foreach (var libraryPath in libraryPaths)
        {
            if (pathToCheck.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase))
            {
                path = pathToCheck;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
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
