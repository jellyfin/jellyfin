using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Metadata;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library;

/// <inheritdoc />
public sealed class VirtualFolderManager : IVirtualFolderManager
{
    private const string ShortcutFileExtension = ".mblink";

    private static readonly string[] _collectionExtensions = [".collection"];

    private readonly ILogger<VirtualFolderManager> _logger;
    private readonly IServerApplicationHost _appHost;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IFileSystem _fileSystem;
    private readonly IProviderManager _providerManager;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly ILibraryManager _libraryManager;
    private readonly ILibraryRefreshManager _libraryRefreshManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualFolderManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
    /// <param name="providerManager">The <see cref="IProviderManager"/>.</param>
    /// <param name="libraryMonitor">The <see cref="ILibraryMonitor"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="libraryRefreshManager">The <see cref="ILibraryRefreshManager"/>.</param>
    public VirtualFolderManager(
        ILogger<VirtualFolderManager> logger,
        IServerApplicationHost appHost,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem,
        IProviderManager providerManager,
        ILibraryMonitor libraryMonitor,
        ILibraryManager libraryManager,
        ILibraryRefreshManager libraryRefreshManager)
    {
        _logger = logger;
        _appHost = appHost;
        _configurationManager = configurationManager;
        _fileSystem = fileSystem;
        _providerManager = providerManager;
        _libraryMonitor = libraryMonitor;
        _libraryManager = libraryManager;
        _libraryRefreshManager = libraryRefreshManager;
    }

    /// <inheritdoc />
    public IEnumerable<VirtualFolderInfo> GetVirtualFolders(bool includeRefreshState = false)
    {
        var topLibraryFolders = _libraryManager.GetUserRootFolder().Children.ToList();
        var refreshQueue = includeRefreshState ? _providerManager.GetRefreshQueue() : null;

        return _fileSystem.GetDirectoryPaths(_configurationManager.ApplicationPaths.DefaultUserViewsPath)
            .Select(dir => GetVirtualFolderInfo(dir, topLibraryFolders, refreshQueue))
            .ToList();
    }

    /// <inheritdoc />
    public async Task AddVirtualFolder(string name, CollectionTypeOptions? collectionType, LibraryOptions options, bool refreshLibrary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        name = _fileSystem.GetValidFilename(name);

        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

        var existingNameCount = 1; // first numbered name will be 2
        var virtualFolderPath = Path.Combine(rootFolderPath, name);
        var originalName = name;
        while (Directory.Exists(virtualFolderPath))
        {
            existingNameCount++;
            name = originalName + existingNameCount;
            virtualFolderPath = Path.Combine(rootFolderPath, name);
        }

        var mediaPathInfos = options.PathInfos;
        var invalidPath = mediaPathInfos.FirstOrDefault(i => !Directory.Exists(i.Path));
        if (invalidPath is not null)
        {
            throw new ArgumentException("The specified path does not exist: " + invalidPath.Path + ".");
        }

        try
        {
            _libraryMonitor.Stop();
            Directory.CreateDirectory(virtualFolderPath);

            if (collectionType is not null)
            {
                var path = Path.Combine(virtualFolderPath, collectionType.ToString()!.ToLowerInvariant() + ".collection");

                await File.WriteAllBytesAsync(path, Array.Empty<byte>()).ConfigureAwait(false);
            }

            CollectionFolder.SaveLibraryOptions(virtualFolderPath, options);
            foreach (var path in mediaPathInfos)
            {
                AddMediaPathInternal(name, path, refreshLibrary, false);
            }
        }
        finally
        {
            await HandleTopLevelMetadataRefresh(refreshLibrary).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RenameVirtualFolder(string name, string newName, bool refreshLibrary)
    {
        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
        var currentPath = Path.Combine(rootFolderPath, name);
        var newPath = Path.Combine(rootFolderPath, newName);

        if (!Directory.Exists(currentPath))
        {
            throw new DirectoryNotFoundException($"{currentPath} does not exist");
        }

        if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(newPath))
        {
            throw new InvalidOperationException($"The media library already exists at {newPath}.");
        }

        try
        {
            // Changing capitalization. Handle windows case insensitivity
            if (string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                var tempPath = Path.Combine(
                    rootFolderPath,
                    Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
                Directory.Move(currentPath, tempPath);
                currentPath = tempPath;
            }

            Directory.Move(currentPath, newPath);
        }
        finally
        {
            CollectionFolder.OnCollectionFolderChange();
            await HandleTopLevelMetadataRefresh(refreshLibrary).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveVirtualFolder(string name, bool refreshLibrary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
        var path = Path.Combine(rootFolderPath, name);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException("The media folder does not exist");
        }

        try
        {
            _libraryMonitor.Stop();
            Directory.Delete(path, true);
        }
        finally
        {
            CollectionFolder.OnCollectionFolderChange();
            await HandleTopLevelMetadataRefresh(refreshLibrary).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void AddMediaPath(string virtualFolderName, MediaPathInfo mediaPath, bool refreshLibrary)
        => AddMediaPathInternal(virtualFolderName, mediaPath, refreshLibrary, true);

    /// <inheritdoc />
    public void UpdateMediaPath(string virtualFolderName, MediaPathInfo mediaPath)
    {
        ArgumentNullException.ThrowIfNull(mediaPath);

        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
        var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

        var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

        SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

        var list = libraryOptions.PathInfos.ToList();
        foreach (var originalPathInfo in list)
        {
            if (string.Equals(mediaPath.Path, originalPathInfo.Path, StringComparison.Ordinal))
            {
                originalPathInfo.NetworkPath = mediaPath.NetworkPath;
                break;
            }
        }

        libraryOptions.PathInfos = list.ToArray();

        CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
    }

    /// <inheritdoc />
    public void RemoveMediaPath(string virtualFolderName, string mediaPath, bool refreshLibrary)
    {
        ArgumentException.ThrowIfNullOrEmpty(virtualFolderName);
        ArgumentException.ThrowIfNullOrEmpty(mediaPath);

        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
        var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);
        if (!Directory.Exists(virtualFolderPath))
        {
            throw new DirectoryNotFoundException(
                string.Format(CultureInfo.InvariantCulture, "The media collection {0} does not exist", virtualFolderName));
        }

        try
        {
            _libraryMonitor.Stop();
            var shortcut = _fileSystem.GetFilePaths(virtualFolderPath, true)
                .Where(i => Path.GetExtension(i.AsSpan()).Equals(ShortcutFileExtension, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(f => _appHost.ExpandVirtualPath(_fileSystem.ResolveShortcut(f)).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(shortcut))
            {
                _fileSystem.DeleteFile(shortcut);
            }

            var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

            libraryOptions.PathInfos = libraryOptions
                .PathInfos
                .Where(i => !string.Equals(i.Path, mediaPath, StringComparison.Ordinal))
                .ToArray();

            CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
        }
        finally
        {
            HandleMetadataRefresh(refreshLibrary);
        }
    }

    private VirtualFolderInfo GetVirtualFolderInfo(string dir, List<BaseItem> allCollectionFolders, HashSet<Guid>? refreshQueue)
    {
        var info = new VirtualFolderInfo
        {
            Name = Path.GetFileName(dir),

            Locations = _fileSystem.GetFilePaths(dir, false)
                .Where(i => Path.GetExtension(i.AsSpan()).Equals(ShortcutFileExtension, StringComparison.OrdinalIgnoreCase))
                .Select(i =>
                {
                    try
                    {
                        return _appHost.ExpandVirtualPath(_fileSystem.ResolveShortcut(i));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resolving shortcut file {File}", i);
                        return null;
                    }
                })
                .Where(i => i is not null)
                .Order()
                .ToArray(),

            CollectionType = GetCollectionType(dir)
        };

        var libraryFolder = allCollectionFolders.FirstOrDefault(i => string.Equals(i.Path, dir, StringComparison.OrdinalIgnoreCase));
        if (libraryFolder is not null)
        {
            var libraryFolderId = libraryFolder.Id.ToString("N", CultureInfo.InvariantCulture);
            info.ItemId = libraryFolderId;
            if (libraryFolder.HasImage(ImageType.Primary))
            {
                info.PrimaryImageItemId = libraryFolderId;
            }

            info.LibraryOptions = _libraryManager.GetLibraryOptions(libraryFolder);

            if (refreshQueue is not null)
            {
                info.RefreshProgress = libraryFolder.GetRefreshProgress();

                info.RefreshStatus = info.RefreshProgress.HasValue ? "Active" : refreshQueue.Contains(libraryFolder.Id) ? "Queued" : "Idle";
            }
        }

        return info;
    }

    private CollectionTypeOptions? GetCollectionType(string path)
    {
        var files = _fileSystem.GetFilePaths(path, _collectionExtensions, true, false);
        foreach (ReadOnlySpan<char> file in files)
        {
            if (Enum.TryParse<CollectionTypeOptions>(Path.GetFileNameWithoutExtension(file), true, out var res))
            {
                return res;
            }
        }

        return null;
    }

    private void AddMediaPathInternal(string virtualFolderName, MediaPathInfo pathInfo, bool refreshLibrary, bool saveLibraryOptions)
    {
        ArgumentNullException.ThrowIfNull(pathInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathInfo.Path);

        var path = pathInfo.Path;

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException("The path does not exist.");
        }

        var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
        var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);
        var shortcutFileName = Path.GetFileNameWithoutExtension(path);
        var lnk = Path.Combine(virtualFolderPath, shortcutFileName + ShortcutFileExtension);

        try
        {
            _libraryMonitor.Stop();
            while (File.Exists(lnk))
            {
                shortcutFileName += "1";
                lnk = Path.Combine(virtualFolderPath, shortcutFileName + ShortcutFileExtension);
            }

            _fileSystem.CreateShortcut(lnk, _appHost.ReverseVirtualPath(path));

            RemoveContentTypeOverrides(path);

            if (saveLibraryOptions)
            {
                var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

                var list = libraryOptions.PathInfos.ToList();
                list.Add(pathInfo);
                libraryOptions.PathInfos = list.ToArray();

                SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

                CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
            }
        }
        finally
        {
            HandleMetadataRefresh(refreshLibrary);
        }
    }

    private void RemoveContentTypeOverrides(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<NameValuePair>? removeList = null;

        foreach (var contentType in _configurationManager.Configuration.ContentTypes)
        {
            if (string.IsNullOrWhiteSpace(contentType.Name)
                || _fileSystem.AreEqual(path, contentType.Name)
                || _fileSystem.ContainsSubPath(path, contentType.Name))
            {
                (removeList ??= new()).Add(contentType);
            }
        }

        if (removeList is not null)
        {
            _configurationManager.Configuration.ContentTypes = _configurationManager.Configuration.ContentTypes
                .Except(removeList)
                .ToArray();

            _configurationManager.SaveConfiguration();
        }
    }

    private void SyncLibraryOptionsToLocations(string virtualFolderPath, LibraryOptions options)
    {
        var topLibraryFolders = _libraryManager.GetUserRootFolder().Children.ToList();
        var info = GetVirtualFolderInfo(virtualFolderPath, topLibraryFolders, null);

        if (info.Locations.Length > 0 && info.Locations.Length != options.PathInfos.Length)
        {
            var list = options.PathInfos.ToList();

            foreach (var location in info.Locations)
            {
                if (!list.Any(i => string.Equals(i.Path, location, StringComparison.Ordinal)))
                {
                    list.Add(new MediaPathInfo(location));
                }
            }

            options.PathInfos = list.ToArray();
        }
    }

    private async Task HandleTopLevelMetadataRefresh(bool refreshLibrary)
    {
        if (refreshLibrary)
        {
            await _libraryRefreshManager.ValidateTopLibraryFolders(CancellationToken.None).ConfigureAwait(false);
        }

        HandleMetadataRefresh(refreshLibrary);
    }

    private async void HandleMetadataRefresh(bool refreshLibrary)
    {
        if (refreshLibrary)
        {
            _libraryRefreshManager.StartScan();
        }
        else
        {
            // Need to add a delay here or directory watchers may still pick up the changes
            await Task.Delay(1000).ConfigureAwait(false);
            _libraryMonitor.Start();
        }
    }
}
