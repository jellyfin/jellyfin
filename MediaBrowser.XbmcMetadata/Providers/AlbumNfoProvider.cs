using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    /// <summary>
    /// Nfo provider for albums.
    /// </summary>
    public class AlbumNfoProvider : BaseNfoProvider<MusicAlbum>
    {
        private readonly ILogger<AlbumNfoProvider> _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{AlbumNfoProvider}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public AlbumNfoProvider(
            ILogger<AlbumNfoProvider> logger,
            IFileSystem fileSystem,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<MusicAlbum> result, string path, CancellationToken cancellationToken)
        {
            new BaseNfoParser<MusicAlbum>(_logger, _config, _providerManager, _userManager, _userDataManager).Fetch(result, path, cancellationToken);
        }

        /// <inheritdoc />
        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "album.nfo"));
    }
}
