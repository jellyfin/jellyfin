using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to re-read creation dates for all library items.
/// </summary>
public class RefreshFilesystemInformation : IDatabaseMigrationRoutine
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<MoveExtractedFiles> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly bool _useFileCreationTimeForDateAdded;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshFilesystemInformation"/> class.
    /// </summary>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public RefreshFilesystemInformation(
        IItemRepository itemRepository,
        ILogger<MoveExtractedFiles> logger,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem)
    {
        _itemRepository = itemRepository;
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
        const int Limit = 500;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();
        var itemsQuery = new InternalItemsQuery
        {
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            Limit = Limit,
            StartIndex = offset,
            EnableTotalRecordCount = true,
        };

        var records = _itemRepository.GetItems(itemsQuery).TotalRecordCount;
        _logger.LogInformation("Checking filesystem information correctness for {Count} items.", records);

        itemsQuery.EnableTotalRecordCount = false;
        List<BaseItem> itemList = [];
        do
        {
            itemsQuery.StartIndex = offset;
            var result = _itemRepository.GetItems(itemsQuery);

            var items = result.Items;
            foreach (var item in items)
            {
                var itemPath = item.Path;
                var modified = false;
                if (!string.IsNullOrEmpty(itemPath))
                {
                    var info = _fileSystem.GetFileSystemInfo(itemPath);
                    var writeTime = info.LastWriteTimeUtc;
                    var itemModificationDate = item.DateModified;
                    if (writeTime != itemModificationDate)
                    {
                        _logger.LogDebug("Reset file system modification date: Old: {Old} - New: {New} - Path: {Path}", itemModificationDate, writeTime, itemPath);
                        item.DateLastModifiedFilesystem = writeTime;
                        if (_useFileCreationTimeForDateAdded)
                        {
                            item.DateCreated = _fileSystem.GetCreationTimeUtc(itemPath);
                        }

                        modified = true;
                    }

                    var filesystemSize = info.Length;
                    if (!info.IsDirectory)
                    {
                        var size = item.Size;
                        if (size != filesystemSize)
                        {
                            _logger.LogDebug("Reset item size: Old: {Old} - New: {New} - Path: {Path}", size, filesystemSize, itemPath);
                            item.Size = filesystemSize;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    itemCount++;
                    itemList.Add(item);
                }
            }

            offset += Limit;
            if (offset % 10_000 == 0)
            {
                _itemRepository.SaveItems(itemList, CancellationToken.None);
                itemList.Clear();
                _logger.LogInformation("Checked: {Count} - Updated: {Items} - Time: {Time}.", offset, itemCount, sw.Elapsed);
            }
        } while (offset < records);

        _logger.LogInformation("Reset filesystem information for {Count} items in {Time}.", itemCount, sw.Elapsed);
    }
}
