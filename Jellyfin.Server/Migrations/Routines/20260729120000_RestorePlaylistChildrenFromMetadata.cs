using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Restores playlist entries from playlist.xml for playlists that lost all of their children.
/// </summary>
[JellyfinMigration("2026-07-29T12:00:00", nameof(RestorePlaylistChildrenFromMetadata))]
internal class RestorePlaylistChildrenFromMetadata : IDatabaseMigrationRoutine
{
    private const string PlaylistTypeName = "MediaBrowser.Controller.Playlists.Playlist";
    private const string PlaylistFileName = "playlist.xml";

    private readonly ILogger<RestorePlaylistChildrenFromMetadata> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;

    public RestorePlaylistChildrenFromMetadata(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost appHost)
    {
        _logger = loggerFactory.CreateLogger<RestorePlaylistChildrenFromMetadata>();
        _dbProvider = dbProvider;
        _appHost = appHost;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        using var context = _dbProvider.CreateDbContext();

        var playlists = context.BaseItems
            .Where(b => b.Type == PlaylistTypeName && b.Path != null)
            .Select(b => new { b.Id, b.Name, b.Path })
            .ToList();

        if (playlists.Count == 0)
        {
            return;
        }

        var childCountByPlaylist = context.LinkedChildren
            .Where(lc => context.BaseItems.Any(b => b.Id.Equals(lc.ParentId) && b.Type == PlaylistTypeName))
            .GroupBy(lc => lc.ParentId)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.ParentId, g => g.Count);

        var pathToIdMap = context.BaseItems
            .Where(b => b.Path != null)
            .Select(b => new { b.Id, b.Path })
            .GroupBy(b => b.Path!)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var restoredPlaylists = 0;
        var restoredEntries = 0;

        foreach (var playlist in playlists)
        {
            // Only directory-based (Jellyfin-managed) playlists keep their entries in playlist.xml.
            // A playlist that is itself a file (.m3u and friends) is re-read by the library scan.
            var playlistPath = _appHost.ExpandVirtualPath(playlist.Path!);
            var metadataPath = Path.Combine(playlistPath, PlaylistFileName);
            if (!Directory.Exists(playlistPath) || !File.Exists(metadataPath))
            {
                continue;
            }

            var storedPaths = ReadEntryPaths(metadataPath, playlist.Id);
            if (storedPaths.Count == 0)
            {
                continue;
            }

            var childCount = childCountByPlaylist.GetValueOrDefault(playlist.Id);
            if (childCount > 0)
            {
                // Merging into a playlist that still has entries would resurrect anything the user
                // removed while the metadata file was not rewritten, and there is no way to tell the
                // two apart. Report the mismatch instead so it can be checked by hand.
                if (storedPaths.Count > childCount)
                {
                    _logger.LogWarning(
                        "Playlist {PlaylistName} ({PlaylistId}) holds {ChildCount} entries but {MetadataPath} lists {StoredCount}. Not restoring automatically.",
                        playlist.Name,
                        playlist.Id,
                        childCount,
                        metadataPath,
                        storedPaths.Count);
                }

                continue;
            }

            var sortOrder = 0;
            foreach (var storedPath in storedPaths)
            {
                if (!pathToIdMap.TryGetValue(storedPath, out var childId))
                {
                    _logger.LogWarning(
                        "Cannot restore entry {EntryPath} of playlist {PlaylistName}: no library item has that path.",
                        storedPath,
                        playlist.Name);
                    continue;
                }

                context.LinkedChildren.Add(new LinkedChildEntity
                {
                    ParentId = playlist.Id,
                    ChildId = childId,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = sortOrder
                });

                sortOrder++;
            }

            if (sortOrder > 0)
            {
                restoredPlaylists++;
                restoredEntries += sortOrder;
                _logger.LogInformation(
                    "Restored {Count} entries of empty playlist {PlaylistName} ({PlaylistId}) from {MetadataPath}.",
                    sortOrder,
                    playlist.Name,
                    playlist.Id,
                    metadataPath);
            }
        }

        if (restoredEntries > 0)
        {
            context.SaveChanges();
            _logger.LogInformation("Restored {EntryCount} entries across {PlaylistCount} playlists.", restoredEntries, restoredPlaylists);
        }
    }

    private List<string> ReadEntryPaths(string metadataPath, Guid playlistId)
    {
        var paths = new List<string>();
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true,
            DtdProcessing = DtdProcessing.Prohibit
        };

        try
        {
            using var reader = XmlReader.Create(metadataPath, settings);
            var inEntry = false;
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (string.Equals(reader.Name, "PlaylistItem", StringComparison.Ordinal))
                {
                    inEntry = true;
                }
                else if (inEntry && string.Equals(reader.Name, "Path", StringComparison.Ordinal))
                {
                    inEntry = false;
                    var value = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        paths.Add(value.Trim());
                    }
                }
            }
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read playlist metadata {MetadataPath} of playlist {PlaylistId}.", metadataPath, playlistId);
        }

        return paths;
    }
}
