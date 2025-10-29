using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Book xml saver.
    /// </summary>
    public class BookXmlSaver : BaseXmlSaver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BookXmlSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction used to read/write files.</param>
        /// <param name="configurationManager">Server configuration manager providing configured options.</param>
        /// <param name="libraryManager">Library manager used for library operations.</param>
        /// <param name="logger">Logger instance.</param>
        public BookXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger<BookXmlSaver> logger)
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

            return item is Book && updateType >= ItemUpdateType.MetadataDownload;
        }

        /// <inheritdoc />
        protected override Task WriteCustomElementsAsync(BaseItem item, XmlWriter writer)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
        {
            // If the item path points to a file, save next to the file with .xml extension.
            // If it's a directory, save as metadata.xml inside the folder.
            var path = item.Path ?? string.Empty;

            if (FileSystem.FileExists(path))
            {
                return Path.ChangeExtension(path, ".xml");
            }

            if (FileSystem.DirectoryExists(path))
            {
                return Path.Combine(path, "metadata.xml");
            }

            // Fallback: save next to path with .xml
            return Path.ChangeExtension(path, ".xml");
        }
    }
}
