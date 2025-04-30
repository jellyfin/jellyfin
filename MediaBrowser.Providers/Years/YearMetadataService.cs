using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Years;

/// <summary>
/// Service to manage year metadata.
/// </summary>
public class YearMetadataService : MetadataService<Year, ItemLookupInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YearMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="pathManager">Instance of the <see cref="IPathManager"/> interface.</param>
    /// <param name="keyframeManager">Instance of the <see cref="IKeyframeManager"/> interface.</param>
    /// <param name="mediaSegmentManager">Instance of the <see cref="IMediaSegmentManager"/> interface.</param>
    public YearMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<YearMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IPathManager pathManager,
        IKeyframeManager keyframeManager,
        IMediaSegmentManager mediaSegmentManager)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, pathManager, keyframeManager, mediaSegmentManager)
    {
    }
}
