using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Class PersonXmlSaver
    /// </summary>
    public class PersonXmlSaver : IMetadataFileSaver
    {
        public string Name
        {
            get
            {
                return XmlProviderUtils.Name;
            }
        }

        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public PersonXmlSaver(IServerConfigurationManager config, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _config = config;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Person && updateType >= ItemUpdateType.MetadataDownload;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var person = (Person)item;

            var builder = new StringBuilder();

            builder.Append("<Item>");

            XmlSaverHelpers.AddCommonNodes(person, _libraryManager, builder);

            if (!string.IsNullOrEmpty(person.PlaceOfBirth))
            {
                builder.Append("<PlaceOfBirth>" + SecurityElement.Escape(person.PlaceOfBirth) + "</PlaceOfBirth>");
            }

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "PlaceOfBirth"
                }, _config, _fileSystem);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "person.xml");
        }
    }
}
