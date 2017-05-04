using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class MovieXmlProvider : BaseXmlProvider<Movie>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        public MovieXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected override void Fetch(MetadataResult<Movie> result, string path, CancellationToken cancellationToken)
        {
            new MovieXmlParser(_logger, _providerManager, XmlReaderSettingsFactory, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return GetXmlFileInfo(info, FileSystem);
        }

        public static FileSystemMetadata GetXmlFileInfo(ItemInfo info, IFileSystem fileSystem)
        {
            var fileInfo = fileSystem.GetFileSystemInfo(info.Path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : fileSystem.GetDirectoryInfo(fileSystem.GetDirectoryName(info.Path));

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, fileSystem.GetFileNameWithoutExtension(info.Path) + ".xml");

            var file = fileSystem.GetFileInfo(specificFile);

            // In a mixed folder, only {moviename}.xml is supported
            if (info.IsInMixedFolder)
            {
                return file;
            }

            // If in it's own folder, prefer movie.xml, but allow the specific file as well
            var movieFile = fileSystem.GetFileInfo(Path.Combine(directoryPath, "movie.xml"));

            return movieFile.Exists ? movieFile : file;
        }
    }
}
