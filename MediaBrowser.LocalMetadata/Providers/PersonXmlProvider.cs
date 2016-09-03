using System.IO;
using System.Threading;
using CommonIO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Providers
{
    public class PersonXmlProvider : BaseXmlProvider<Person>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public PersonXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Person> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Person>(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "person.xml"));
        }
    }
}
