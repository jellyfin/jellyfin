using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class ArtistNfoSaver : BaseNfoSaver
    {
        public ArtistNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => Path.Combine(item.Path, "artist.nfo");

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => "artist";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is MusicArtist && updateType >= MinimumUpdateType;

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var artist = (MusicArtist)item;

            if (artist.EndDate.HasValue)
            {
                var formatString = ConfigurationManager.GetNfoConfiguration().ReleaseDateFormat;

                writer.WriteElementString("disbanded", artist.EndDate.Value.ToLocalTime().ToString(formatString));
            }

            var albums = artist
                .GetRecursiveChildren(i => i is MusicAlbum);

            AddAlbums(albums, writer);
        }

        private void AddAlbums(IList<BaseItem> albums, XmlWriter writer)
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
                    writer.WriteElementString("year", album.ProductionYear.Value.ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }
        }

        /// <inheritdoc />
        protected override List<string> GetTagsUsed(BaseItem item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(new string[]
            {
                "album",
                "disbanded"
            });

            return list;
        }
    }
}
