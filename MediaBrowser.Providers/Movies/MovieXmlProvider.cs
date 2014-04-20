using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Movies
{
    public class MovieXmlProvider : BaseXmlProvider<Movie>
    {
        private readonly ILogger _logger;

        public MovieXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Movie> result, string path, CancellationToken cancellationToken)
        {
            new MovieXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return GetXmlFileInfo(info, FileSystem);
        }

        public static FileInfo GetXmlFileInfo(ItemInfo info, IFileSystem fileSystem)
        {
            var fileInfo = fileSystem.GetFileSystemInfo(info.Path);

            var directoryInfo = fileInfo as DirectoryInfo;

            if (directoryInfo == null)
            {
                directoryInfo = new DirectoryInfo(Path.GetDirectoryName(info.Path));
            }

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(info.Path) + ".xml");

            var file = new FileInfo(specificFile);

            // In a mixed folder, only {moviename}.xml is supported
            if (info.IsInMixedFolder)
            {
                return file;
            }

            // If in it's own folder, prefer movie.xml, but allow the specific file as well
            var movieFile = new FileInfo(Path.Combine(directoryPath, "movie.xml"));

            return movieFile.Exists ? movieFile : file;
        }
    }
}
