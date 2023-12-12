using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Emby.Server.Implementations;

/// <inheritdoc />
public class SystemManager : ISystemManager
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IServerApplicationPaths _applicationPaths;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IStartupOptions _startupOptions;
    private readonly IInstallationManager _installationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemManager"/> class.
    /// </summary>
    /// <param name="applicationLifetime">Instance of <see cref="IHostApplicationLifetime"/>.</param>
    /// <param name="applicationHost">Instance of <see cref="IServerApplicationHost"/>.</param>
    /// <param name="applicationPaths">Instance of <see cref="IServerApplicationPaths"/>.</param>
    /// <param name="configurationManager">Instance of <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="startupOptions">Instance of <see cref="IStartupOptions"/>.</param>
    /// <param name="installationManager">Instance of <see cref="IInstallationManager"/>.</param>
    public SystemManager(
        IHostApplicationLifetime applicationLifetime,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IServerConfigurationManager configurationManager,
        IStartupOptions startupOptions,
        IInstallationManager installationManager)
    {
        _applicationLifetime = applicationLifetime;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _configurationManager = configurationManager;
        _startupOptions = startupOptions;
        _installationManager = installationManager;
    }

    /// <inheritdoc />
    public SystemInfo GetSystemInfo(HttpRequest request)
    {
        return new SystemInfo
        {
            HasPendingRestart = _applicationHost.HasPendingRestart,
            IsShuttingDown = _applicationLifetime.ApplicationStopping.IsCancellationRequested,
            Version = _applicationHost.ApplicationVersionString,
            WebSocketPortNumber = _applicationHost.HttpPort,
            CompletedInstallations = _installationManager.CompletedInstallations.ToArray(),
            Id = _applicationHost.SystemId,
            ProgramDataPath = _applicationPaths.ProgramDataPath,
            WebPath = _applicationPaths.WebPath,
            LogPath = _applicationPaths.LogDirectoryPath,
            ItemsByNamePath = _applicationPaths.InternalMetadataPath,
            InternalMetadataPath = _applicationPaths.InternalMetadataPath,
            CachePath = _applicationPaths.CachePath,
            TranscodingTempPath = _configurationManager.GetTranscodePath(),
            ServerName = _applicationHost.FriendlyName,
            LocalAddress = _applicationHost.GetSmartApiUrl(request),
            SupportsLibraryMonitor = true,
            PackageName = _startupOptions.PackageName,
            CastReceiverApplications = _configurationManager.Configuration.CastReceiverApplications
        };
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Restart() => ShutdownInternal(true);

    /// <inheritdoc />
    public void Shutdown() => ShutdownInternal(false);

    private void ShutdownInternal(bool restart)
    {
        Task.Run(async () =>
        {
            await Task.Delay(100).ConfigureAwait(false);
            _applicationHost.ShouldRestart = restart;
            _applicationLifetime.StopApplication();
        });
    }
}
