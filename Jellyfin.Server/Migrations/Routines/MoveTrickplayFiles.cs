using System;
using System.Globalization;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.IO;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move trickplay files to the new directory.
/// </summary>
public class MoveTrickplayFiles : IMigrationRoutine
{
    private readonly ITrickplayManager _trickplayManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTrickplayFiles"/> class.
    /// </summary>
    /// <param name="trickplayManager">Instance of the <see cref="ITrickplayManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public MoveTrickplayFiles(ITrickplayManager trickplayManager, IFileSystem fileSystem, ILibraryManager libraryManager)
    {
        _trickplayManager = trickplayManager;
        _fileSystem = fileSystem;
        _libraryManager = libraryManager;
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
        var trickplayItems = _trickplayManager.GetTrickplayItemsAsync().GetAwaiter().GetResult();
        foreach (var itemId in trickplayItems)
        {
            var resolutions = _trickplayManager.GetTrickplayResolutions(itemId).GetAwaiter().GetResult();
            var item = _libraryManager.GetItemById(itemId);
            if (item is null)
            {
                continue;
            }

            foreach (var resolution in resolutions)
            {
                var oldPath = GetOldTrickplayDirectory(item, resolution.Key);
                var newPath = _trickplayManager.GetTrickplayDirectory(item, resolution.Value.TileWidth, resolution.Value.TileHeight, resolution.Value.Width, false);
                if (_fileSystem.DirectoryExists(oldPath))
                {
                    _fileSystem.MoveDirectory(oldPath, newPath);
                }
            }
        }
    }

    private string GetOldTrickplayDirectory(BaseItem item, int? width = null)
    {
        var path = Path.Combine(item.GetInternalMetadataPath(), "trickplay");

        return width.HasValue ? Path.Combine(path, width.Value.ToString(CultureInfo.InvariantCulture)) : path;
    }
}
