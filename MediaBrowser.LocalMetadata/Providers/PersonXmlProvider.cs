using System.IO;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Parsers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Providers
{
    //public class PersonXmlProvider : BaseXmlProvider<Person>
    //{
    //    private readonly ILogger _logger;
    //    private readonly IProviderManager _providerManager;
    //    protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

    //    public PersonXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
    //        : base(fileSystem)
    //    {
    //        _logger = logger;
    //        _providerManager = providerManager;
    //        XmlReaderSettingsFactory = xmlReaderSettingsFactory;
    //    }

    //    protected override void Fetch(MetadataResult<Person> result, string path, CancellationToken cancellationToken)
    //    {
    //        new BaseItemXmlParser<Person>(_logger, _providerManager, XmlReaderSettingsFactory, FileSystem).Fetch(result, path, cancellationToken);
    //    }

    //    protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
    //    {
    //        return directoryService.GetFile(Path.Combine(info.Path, "person.xml"));
    //    }
    //}
}
