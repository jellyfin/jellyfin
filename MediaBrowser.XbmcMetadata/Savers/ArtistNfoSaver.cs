using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class ArtistNfoSaver : BaseNfoSaver
    {
        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "artist.nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "artist";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is MusicArtist && updateType >= MinimumUpdateType;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var artist = (MusicArtist)item;

            if (artist.EndDate.HasValue)
            {
                var formatString = ConfigurationManager.GetNfoConfiguration().ReleaseDateFormat;

                writer.WriteElementString("disbanded", artist.EndDate.Value.ToLocalTime().ToString(formatString));
            }
            
            var albums = artist
                .GetRecursiveChildren(i => i is MusicAlbum)
                .Cast<MusicAlbum>()
                .ToList();

            AddAlbums(albums, writer);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        private void AddAlbums(IEnumerable<MusicAlbum> albums, XmlWriter writer)
        {
            foreach (var album in albums)
            {
                writer.WriteStartElement("album");

                if (!string.IsNullOrEmpty(album.Name))
                {
                    writer.WriteElementString("title", album.Name);
                }

                if (album.ProductionYear.HasValue)
                {
                    writer.WriteElementString("year", album.ProductionYear.Value.ToString(UsCulture));
                }

                writer.WriteEndElement();
            }
        }

        protected override List<string> GetTagsUsed(IHasMetadata item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(new string[]
            {
                "album",
                "disbanded"
            });
            return list;
        }

        public ArtistNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}