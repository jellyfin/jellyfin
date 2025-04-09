using System;
using System.Diagnostics;
using System.Linq;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to re-read creation dates for all library items.
/// </summary>
public class RefreshFilesystemInformation : IDatabaseMigrationRoutine
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _applicationHost;
    private readonly ILogger<RefreshFilesystemInformation> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly bool _useFileCreationTimeForDateAdded;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshFilesystemInformation"/> class.
    /// </summary>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public RefreshFilesystemInformation(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        ILogger<RefreshFilesystemInformation> logger,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem)
    {
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _logger = logger;
        _fileSystem = fileSystem;
        _useFileCreationTimeForDateAdded = configurationManager.GetMetadataConfiguration().UseFileCreationTimeForDateAdded;
    }

    /// <inheritdoc />
    public Guid Id => new("32E762EB-4918-45CE-A44C-C801F66B877D");

    /// <inheritdoc />
    public string Name => "RefreshFilesystemInformation";

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 5000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Where(b => !string.IsNullOrEmpty(b.Path)).Count();
        _logger.LogInformation("Checking filesystem information correctness for {Count} items.", records);
        do
        {
            var items = context.BaseItems.Where(b => !string.IsNullOrEmpty(b.Path)).OrderBy(b => b.Id).Skip(offset).Take(Limit).ToList();
            foreach (var item in items)
            {
                var itemPath = _applicationHost.ExpandVirtualPath(item.Path);
                var modified = false;
                if (!string.IsNullOrEmpty(itemPath) && _fileSystem.IsPathFile(itemPath))
                {
                    var info = _fileSystem.GetFileSystemInfo(itemPath);
                    if (!info.Exists)
                    {
                        continue;
                    }

                    var writeTime = info.LastWriteTimeUtc;
                    if (writeTime == item.DateModified)
                    {
                        item.DateModified = writeTime;
                    }

                    var itemModificationTime = item.DateModified;
                    if (writeTime != itemModificationTime)
                    {
                        _logger.LogDebug("Reset file system modification date: Old: {Old} - New: {New} - Path: {Path}", itemModificationTime, writeTime, itemPath);
                        item.DateModified = writeTime;
                        item.DateModified = writeTime;
                        if (_useFileCreationTimeForDateAdded)
                        {
                            item.DateCreated = _fileSystem.GetCreationTimeUtc(itemPath);
                        }

                        modified = true;
                    }

                    if (!info.IsDirectory)
                    {
                        var filesystemSize = info.Length;
                        var size = item.Size;
                        if (filesystemSize != (size ?? 0))
                        {
                            _logger.LogDebug("Reset file size: Old: {Old} - New: {New} - Path: {Path}", size, filesystemSize, itemPath);
                            item.Size = filesystemSize;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    itemCount++;
                }
            }

            offset += Limit;
            if (offset % 5_000 == 0)
            {
                _logger.LogInformation("Checked: {Count} - Updated: {Items} - Time: {Time}.", offset, itemCount, sw.Elapsed);
            }
        } while (offset < records);

        context.SaveChanges();
        _logger.LogInformation("Reset filesystem information for {Count} items in {Time}.", itemCount, sw.Elapsed);
    }
}
