using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to re-read creation dates for library items with internal metadata paths.
/// </summary>
[JellyfinMigration("2025-04-20T23:00:00", nameof(RefreshInternalDateModified))]
public class RefreshInternalDateModified : IDatabaseMigrationRoutine
{
    private readonly ILogger<RefreshInternalDateModified> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IServerApplicationHost _applicationHost;
    private readonly bool _useFileCreationTimeForDateAdded;

    private IReadOnlyList<string> _internalTypes = [
         typeof(Genre).FullName!,
         typeof(MusicGenre).FullName!,
         typeof(MusicArtist).FullName!,
         typeof(People).FullName!,
         typeof(Studio).FullName!
    ];

    private IReadOnlyList<string> _internalPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshInternalDateModified"/> class.
    /// </summary>
    /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public RefreshInternalDateModified(
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IServerConfigurationManager configurationManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        ILogger<RefreshInternalDateModified> logger,
        IFileSystem fileSystem)
    {
        _dbProvider = dbProvider;
        _logger = logger;
        _fileSystem = fileSystem;
        _applicationHost = applicationHost;
        _internalPaths = [
            applicationPaths.ArtistsPath,
            applicationPaths.GenrePath,
            applicationPaths.MusicGenrePath,
            applicationPaths.StudioPath,
            applicationPaths.PeoplePath
        ];
        _useFileCreationTimeForDateAdded = configurationManager.GetMetadataConfiguration().UseFileCreationTimeForDateAdded;
    }

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 5000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Count(b => _internalTypes.Contains(b.Type));
        _logger.LogInformation("Checking if {Count} potentially internal items require refreshed DateModified", records);

        do
        {
            var results = context.BaseItems
                            .Where(b => _internalTypes.Contains(b.Type))
                            .OrderBy(e => e.Id)
                            .Skip(offset)
                            .Take(Limit)
                            .ToList();

            foreach (var item in results)
            {
                var itemPath = item.Path;
                if (itemPath is not null)
                {
                    var realPath = _applicationHost.ExpandVirtualPath(item.Path);
                    if (_internalPaths.Any(path => realPath.StartsWith(path, StringComparison.Ordinal)))
                    {
                        var writeTime = _fileSystem.GetLastWriteTimeUtc(realPath);
                        var itemModificationTime = item.DateModified;
                        if (writeTime != itemModificationTime)
                        {
                            _logger.LogDebug("Reset file modification date: Old: {Old} - New: {New} - Path: {Path}", itemModificationTime, writeTime, realPath);
                            item.DateModified = writeTime;
                            if (_useFileCreationTimeForDateAdded)
                            {
                                item.DateCreated = _fileSystem.GetCreationTimeUtc(realPath);
                            }

                            itemCount++;
                        }
                    }
                }
            }

            offset += Limit;
            if (offset > records)
            {
                offset = records;
            }

            _logger.LogInformation("Checked: {Count} - Refreshed: {Items} - Time: {Time}", offset, itemCount, sw.Elapsed);
        } while (offset < records);

        context.SaveChanges();

        _logger.LogInformation("Refreshed DateModified for {Count} items in {Time}", itemCount, sw.Elapsed);
    }
}
