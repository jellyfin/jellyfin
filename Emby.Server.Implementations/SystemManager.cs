using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.StorageHelpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Emby.Server.Implementations;

/// <summary>
/// Manages system-level information and storage reporting for the server.
/// </summary>
public class SystemManager : ISystemManager
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IServerApplicationPaths _applicationPaths;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IStartupOptions _startupOptions;
    private readonly IInstallationManager _installationManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemManager"/> class.
    /// </summary>
    /// <param name="applicationLifetime">Application lifetime.</param>
    /// <param name="applicationHost">Application host.</param>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="configurationManager">Configuration manager.</param>
    /// <param name="startupOptions">Startup options.</param>
    /// <param name="installationManager">Installation manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    public SystemManager(
        IHostApplicationLifetime applicationLifetime,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IServerConfigurationManager configurationManager,
        IStartupOptions startupOptions,
        IInstallationManager installationManager,
        ILibraryManager libraryManager)
    {
        _applicationLifetime = applicationLifetime;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _configurationManager = configurationManager;
        _startupOptions = startupOptions;
        _installationManager = installationManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets overall system information.
    /// </summary>
    /// <param name="request">The current HTTP request (used to build local addresses).</param>
    /// <returns>A <see cref="SystemInfo"/> object describing the system.</returns>
    public SystemInfo GetSystemInfo(HttpRequest request)
    {
        return new SystemInfo
        {
            HasPendingRestart = _applicationHost.HasPendingRestart,
            IsShuttingDown = _applicationLifetime.ApplicationStopping.IsCancellationRequested,
            Version = _applicationHost.ApplicationVersionString,
            ProductName = _applicationHost.Name,
            WebSocketPortNumber = _applicationHost.HttpPort,
            CompletedInstallations = _installationManager.CompletedInstallations.ToArray(),
            Id = _applicationHost.SystemId,
#pragma warning disable CS0618
            ProgramDataPath = _applicationPaths.ProgramDataPath,
            WebPath = _applicationPaths.WebPath,
            LogPath = _applicationPaths.LogDirectoryPath,
            ItemsByNamePath = _applicationPaths.InternalMetadataPath,
            InternalMetadataPath = _applicationPaths.InternalMetadataPath,
            CachePath = _applicationPaths.CachePath,
            TranscodingTempPath = _configurationManager.GetTranscodePath(),
#pragma warning restore CS0618
            ServerName = _applicationHost.FriendlyName,
            LocalAddress = _applicationHost.GetSmartApiUrl(request),
            StartupWizardCompleted = _configurationManager.CommonConfiguration.IsStartupWizardCompleted,
            SupportsLibraryMonitor = true,
            PackageName = _startupOptions.PackageName,
            CastReceiverApplications = _configurationManager.Configuration.CastReceiverApplications
        };
    }

    /// <summary>
    /// Gets storage information for common Jellyfin folders and libraries.
    /// This method will attempt to compute cached per-folder sizes (may block briefly while priming cache).
    /// </summary>
    /// <returns>A <see cref="SystemStorageInfo"/> object with folder and library storage details.</returns>
    public SystemStorageInfo GetSystemStorageInfo()
    {
        var virtualFolderInfos = _libraryManager
            .GetVirtualFolders()
            .Where(e => !string.IsNullOrWhiteSpace(e.ItemId))
            .Select(e => new LibraryStorageInfo
            {
                Id = Guid.Parse(e.ItemId),
                Name = e.Name,
                Folders = e.Locations.Select(f => StorageHelper.GetFreeSpaceOf(f)).ToArray()
            })
            .ToArray();

        var systemStorage = new SystemStorageInfo
        {
            ProgramDataFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.ProgramDataPath),
            WebFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.WebPath),
            LogFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.LogDirectoryPath),
            ImageCacheFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.ImageCachePath),
            InternalMetadataFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.InternalMetadataPath),
            CacheFolder = StorageHelper.GetFreeSpaceOf(_applicationPaths.CachePath),
            TranscodingTempFolder = StorageHelper.GetFreeSpaceOf(_configurationManager.GetTranscodePath()),
            Libraries = virtualFolderInfos
        };

        var allPaths = new List<string?>
        {
            systemStorage.ProgramDataFolder?.Path,
            systemStorage.WebFolder?.Path,
            systemStorage.LogFolder?.Path,
            systemStorage.ImageCacheFolder?.Path,
            systemStorage.InternalMetadataFolder?.Path,
            systemStorage.CacheFolder?.Path,
            systemStorage.TranscodingTempFolder?.Path
        };

        foreach (var lib in systemStorage.Libraries ?? Array.Empty<LibraryStorageInfo>())
        {
            if (lib?.Folders != null)
            {
                foreach (var f in lib.Folders)
                {
                    allPaths.Add(f?.Path);
                }
            }
        }

        var distinctPaths = allPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var computeTasks = distinctPaths
            .Select(p => StorageHelper.ComputeAndCacheFolderSizeAsync(p!, CancellationToken.None))
            .ToArray();

        Task.WhenAll(computeTasks).GetAwaiter().GetResult();

        void PopulateFolderInfo(FolderStorageInfo? fi)
        {
            if (fi == null || string.IsNullOrWhiteSpace(fi.Path))
            {
                return;
            }

            fi.FolderSizeBytes = StorageHelper.GetCachedFolderSize(fi.Path);
            fi.DriveTotalBytes = StorageHelper.GetDriveTotalBytes(fi.Path);
        }

        if (systemStorage.ProgramDataFolder != null)
        {
            PopulateFolderInfo(systemStorage.ProgramDataFolder);
        }

        if (systemStorage.WebFolder != null)
        {
            PopulateFolderInfo(systemStorage.WebFolder);
        }

        if (systemStorage.LogFolder != null)
        {
            PopulateFolderInfo(systemStorage.LogFolder);
        }

        if (systemStorage.ImageCacheFolder != null)
        {
            PopulateFolderInfo(systemStorage.ImageCacheFolder);
        }

        if (systemStorage.InternalMetadataFolder != null)
        {
            PopulateFolderInfo(systemStorage.InternalMetadataFolder);
        }

        if (systemStorage.CacheFolder != null)
        {
            PopulateFolderInfo(systemStorage.CacheFolder);
        }

        if (systemStorage.TranscodingTempFolder != null)
        {
            PopulateFolderInfo(systemStorage.TranscodingTempFolder);
        }

        if (systemStorage.Libraries != null)
        {
            foreach (var lib in systemStorage.Libraries)
            {
                if (lib?.Folders == null)
                {
                    continue;
                }

                foreach (var f in lib.Folders)
                {
                    PopulateFolderInfo(f);
                }
            }
        }

        return systemStorage;
    }

    /// <summary>
    /// Gets public-facing system information.
    /// </summary>
    /// <param name="request">The current HTTP request (used to build local addresses).</param>
    /// <returns>A <see cref="PublicSystemInfo"/> object.</returns>
    public PublicSystemInfo GetPublicSystemInfo(HttpRequest request)
    {
        return new PublicSystemInfo
        {
            Version = _applicationHost.ApplicationVersionString,
            ProductName = _applicationHost.Name,
            Id = _applicationHost.SystemId,
            ServerName = _applicationHost.FriendlyName,
            LocalAddress = _applicationHost.GetSmartApiUrl(request),
            StartupWizardCompleted = _configurationManager.CommonConfiguration.IsStartupWizardCompleted
        };
    }

    /// <summary>
    /// Restarts the server.
    /// </summary>
    public void Restart()
    {
        ShutdownInternal(true);
    }

    /// <summary>
    /// Shuts down the server.
    /// </summary>
    public void Shutdown()
    {
        ShutdownInternal(false);
    }

    private void ShutdownInternal(bool restart)
    {
        Task.Run(
            async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                _applicationHost.ShouldRestart = restart;
                _applicationLifetime.StopApplication();
            });
    }
}
