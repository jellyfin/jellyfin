using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    /// <summary>
    /// Nfo saver for albums.
    /// </summary>
    public class AlbumNfoSaver : BaseNfoSaver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumNfoSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="configurationManager">the server configuration manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataManager">The user data manager.</param>
        /// <param name="logger">The logger.</param>
        public AlbumNfoSaver(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger<AlbumNfoSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => Path.Combine(item.Path, "album.nfo");

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => "album";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is MusicAlbum && updateType >= MinimumUpdateType;

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var album = (MusicAlbum)item;

            foreach (var artist in album.Artists)
            {
                writer.WriteElementString("artist", artist);
            }

            foreach (var artist in album.AlbumArtists)
            {
                writer.WriteElementString("albumartist", artist);
            }

            AddTracks(album.Tracks, writer);
        }

        private void AddTracks(IEnumerable<BaseItem> tracks, XmlWriter writer)
        {
            foreach (var track in tracks.OrderBy(i => i.ParentIndexNumber ?? 0).ThenBy(i => i.IndexNumber ?? 0))
            {
                writer.WriteStartElement("track");

                if (track.IndexNumber.HasValue)
                {
                    writer.WriteElementString("position", track.IndexNumber.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrEmpty(track.Name))
                {
                    writer.WriteElementString("title", track.Name);
                }

                if (track.RunTimeTicks.HasValue)
                {
                    var time = TimeSpan.FromTicks(track.RunTimeTicks.Value).ToString(@"mm\:ss", CultureInfo.InvariantCulture);

                    writer.WriteElementString("duration", time);
                }

                writer.WriteEndElement();
            }
        }

        /// <inheritdoc />
        protected override List<string> GetTagsUsed(BaseItem item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(
                new string[]
                {
                    "track",
                    "artist",
                    "albumartist"
                });

            return list;
        }
    }
}
