using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
{
    ///// <summary>
    ///// Class PersonXmlSaver
    ///// </summary>
    //public class PersonXmlSaver : BaseXmlSaver
    //{
    //    public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
    //    {
    //        if (!item.SupportsLocalMetadata)
    //        {
    //            return false;
    //        }

    //        return item is Person && updateType >= ItemUpdateType.MetadataDownload;
    //    }

    //    protected override List<string> GetTagsUsed()
    //    {
    //        var list = new List<string>
    //        {
    //            "PlaceOfBirth"
    //        };

    //        return list;
    //    }

    //    protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
    //    {
    //        var person = (Person)item;

    //        if (person.ProductionLocations.Count > 0)
    //        {
    //            writer.WriteElementString("PlaceOfBirth", person.ProductionLocations[0]);
    //        }
    //    }

    //    protected override string GetLocalSavePath(IHasMetadata item)
    //    {
    //        return Path.Combine(item.Path, "person.xml");
    //    }

    //    public PersonXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
    //    {
    //    }
    //}
}
