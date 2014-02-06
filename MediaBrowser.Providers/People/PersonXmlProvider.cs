using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.People
{
    public class PersonXmlProvider : BaseXmlProvider<Person>
    {
        private readonly ILogger _logger;

        public PersonXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(Person item, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Person>(_logger).Fetch(item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return new FileInfo(Path.Combine(info.Path, "person.xml"));
        }
    }
}
