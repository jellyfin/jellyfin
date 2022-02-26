using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Box set xml saver.
    /// </summary>
    public class BoxSetXmlSaver : BaseXmlSaver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoxSetXmlSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BoxSetXmlSaver}"/> interface.</param>
        public BoxSetXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger<BoxSetXmlSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, logger)
        {
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is BoxSet && updateType >= ItemUpdateType.MetadataDownload;
        }

        /// <inheritdoc />
        protected override Task WriteCustomElementsAsync(BaseItem item, XmlWriter writer)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "collection.xml");
        }
    }
}
