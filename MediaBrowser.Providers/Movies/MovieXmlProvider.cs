using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class MovieXmlProvider : BaseXmlProvider, ILocalMetadataProvider<Movie>
    {
        private readonly ILogger _logger;

        public MovieXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlFile(path).FullName;

            var result = new MetadataResult<Movie>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                result.Item = new Movie();

                new MovieXmlParser(_logger).Fetch(result.Item, path, cancellationToken);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            finally
            {
                XmlParsingResourcePool.Release();
            }

            return result;
        }

        public string Name
        {
            get { return "Media Browser Xml"; }
        }

        protected override FileInfo GetXmlFile(string path)
        {
            return GetXmlFileInfo(path, FileSystem);
        }

        public static FileInfo GetXmlFileInfo(string path, IFileSystem _fileSystem)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo as DirectoryInfo;

            if (directoryInfo == null)
            {
                directoryInfo = new DirectoryInfo(Path.GetDirectoryName(path));
            }

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".xml");

            var file = new FileInfo(specificFile);

            return file.Exists ? file : new FileInfo(Path.Combine(directoryPath, "movie.xml"));
        }
    }
}
