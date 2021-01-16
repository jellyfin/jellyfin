using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.KodiMetadata.Models;
using Jellyfin.KodiMetadata.Providers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// The music video nfo metadata saver.
    /// </summary>
    public class MusicVideoNfoSaver : BaseNfoSaver<MusicVideo, MusicVideoNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicVideoNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public MusicVideoNfoSaver(
            ILogger<BaseNfoSaver<MusicVideo, MusicVideoNfo>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
            : base(logger, xmlSerializer, fileSystem, configurationManager, libraryManager, userManager, userDataManager)
        {
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            return MusicVideoNfoProvider.GetMusicVideoSavePaths(new ItemInfo(item))
                .FirstOrDefault()
                   ?? Path.ChangeExtension(item.Path, ".nfo");
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            // Check parent for null to avoid running this against things like video backdrops
            if (item is Video video && !(item is Episode) && !video.ExtraType.HasValue)
            {
                return updateType >= MinimumUpdateType;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void MapJellyfinToNfoObject(MusicVideo? item, MusicVideoNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            nfo.Album = item.Album;

            var artistList = new List<string>();
            foreach (var artist in item.Artists)
            {
                artistList.Add(artist);
            }

            nfo.Artists = artistList.ToArray();
            base.MapJellyfinToNfoObject(item, nfo);
        }
    }
}
