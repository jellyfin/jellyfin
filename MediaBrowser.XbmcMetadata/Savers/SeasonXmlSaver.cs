using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        public SeasonXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        public string Name
        {
            get
            {
                return "Xbmc Nfo";
            }
        }

        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<season>");

            var season = (Season)item;

            if (season.IndexNumber.HasValue)
            {
                builder.Append("<seasonnumber>" + SecurityElement.Escape(season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)) + "</seasonnumber>");
            }

            XmlSaverHelpers.AddCommonNodes((Season)item, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            builder.Append("</season>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
            {
                "seasonnumber"
            });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            if (!(item is Season))
            {
                return false;
            }

            return updateType >= ItemUpdateType.MetadataDownload || (updateType >= ItemUpdateType.MetadataImport && File.Exists(GetSavePath(item)));
        }
    }
}
