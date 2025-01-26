using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move trickplay files to the new directory.
/// </summary>
public class MoveTrickplayFiles : IMigrationRoutine
{
    private readonly ITrickplayManager _trickplayManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MoveTrickplayFiles> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTrickplayFiles"/> class.
    /// </summary>
    /// <param name="trickplayManager">Instance of the <see cref="ITrickplayManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">The logger.</param>
    public MoveTrickplayFiles(ITrickplayManager trickplayManager, IFileSystem fileSystem, ILibraryManager libraryManager, ILogger<MoveTrickplayFiles> logger)
    {
        _trickplayManager = trickplayManager;
        _fileSystem = fileSystem;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Guid Id => new("4EF123D5-8EFF-4B0B-869D-3AED07A60E1B");

    /// <inheritdoc />
    public string Name => "MoveTrickplayFiles";

    /// <inheritdoc />
    public bool PerformOnNewInstall => true;

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 100;
        int itemCount = 0, offset = 0, previousCount;

        var sw = Stopwatch.StartNew();
        var trickplayQuery = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false
        };

        do
        {
            var trickplayInfos = _trickplayManager.GetTrickplayItemsAsync(Limit, offset).GetAwaiter().GetResult();
            previousCount = trickplayInfos.Count;
            offset += Limit;

            trickplayQuery.ItemIds = trickplayInfos.Select(i => i.ItemId).Distinct().ToArray();
            var items = _libraryManager.GetItemList(trickplayQuery);
            foreach (var trickplayInfo in trickplayInfos)
            {
                var item = items.OfType<Video>().FirstOrDefault(i => i.Id.Equals(trickplayInfo.ItemId));
                if (item is null)
                {
                    continue;
                }

                if (++itemCount % 1_000 == 0)
                {
                    _logger.LogInformation("Moved {Count} items in {Time}", itemCount, sw.Elapsed);
                }

                var oldPath = GetOldTrickplayDirectory(item, trickplayInfo.Width);
                var newPath = _trickplayManager.GetTrickplayDirectory(item, trickplayInfo.TileWidth, trickplayInfo.TileHeight, trickplayInfo.Width, false);
                if (_fileSystem.DirectoryExists(oldPath))
                {
                    _fileSystem.MoveDirectory(oldPath, newPath);
                }
            }
        } while (previousCount == Limit);

        _logger.LogInformation("Moved {Count} items in {Time}", itemCount, sw.Elapsed);
    }

    private string GetOldTrickplayDirectory(BaseItem item, int? width = null)
    {
        var path = Path.Combine(item.GetInternalMetadataPath(), "trickplay");

        return width.HasValue ? Path.Combine(path, width.Value.ToString(CultureInfo.InvariantCulture)) : path;
    }
}
