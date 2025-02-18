using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers;

/// <summary>
/// Nfo provider for music videos.
/// </summary>
public class MusicVideoNfoProvider : BaseVideoNfoProvider<MusicVideo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicVideoNfoProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{SeasonNfoProvider}"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
    public MusicVideoNfoProvider(
        ILogger<MusicVideoNfoProvider> logger,
        IFileSystem fileSystem,
        IConfigurationManager config,
        IProviderManager providerManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IDirectoryService directoryService)
        : base(logger, fileSystem, config, providerManager, userManager, userDataManager, directoryService)
    {
    }
}
