using System.IO;
using System.Threading;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Providers
{
    /// <summary>
    /// Class BoxSetXmlProvider.
    /// </summary>
    public class BoxSetXmlProvider : BaseXmlProvider<BoxSet>
    {
        private readonly ILogger<BoxSetXmlParser> _logger;
        private readonly IProviderManager _providerManager;
        private readonly IBaseItemManager _baseItemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxSetXmlProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BoxSetXmlParser}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="baseItemManager">Instance of the <see cref="IBaseItemManager"/> interface.</param>
        public BoxSetXmlProvider(
            IFileSystem fileSystem,
            ILogger<BoxSetXmlParser> logger,
            IProviderManager providerManager,
            IBaseItemManager baseItemManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            _baseItemManager = baseItemManager;
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<BoxSet> result, string path, CancellationToken cancellationToken)
        {
            new BoxSetXmlParser(_logger, _providerManager, _baseItemManager).Fetch(result, path, cancellationToken);
        }

        /// <inheritdoc />
        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "collection.xml"));
        }
    }
}
