using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.People
{
    public class PersonXmlProvider : BaseXmlProvider, ILocalMetadataProvider<Person>
    {
        private readonly ILogger _logger;

        public PersonXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        public async Task<MetadataResult<Person>> GetMetadata(string path, CancellationToken cancellationToken)
        {
            path = GetXmlPath(path);

            var result = new MetadataResult<Person>();

            await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var person = new Person();

                new BaseItemXmlParser<Person>(_logger).Fetch(person, path, cancellationToken);
                result.HasMetadata = true;
                result.Item = person;
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

        protected override string GetXmlPath(string path)
        {
            return Path.Combine(path, "person.xml");
        }
    }
}
