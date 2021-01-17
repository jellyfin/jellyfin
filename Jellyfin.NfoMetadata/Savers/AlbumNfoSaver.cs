using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.NfoMetadata.Models;
using Jellyfin.NfoMetadata.Providers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.NfoMetadata.Savers
{
    /// <summary>
    /// The music album nfo metadata saver.
    /// </summary>
    public class AlbumNfoSaver : BaseNfoSaver<MusicAlbum, AlbumNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public AlbumNfoSaver(
            ILogger<BaseNfoSaver<MusicAlbum, AlbumNfo>> logger,
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
        protected override void MapJellyfinToNfoObject(MusicAlbum item, AlbumNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            nfo.Artists = item.Artists.ToArray();
            nfo.AlbumArtists = item.AlbumArtists.ToArray();

            List<TrackNfo> trackNfos = new List<TrackNfo>();
            foreach (var track in item.Tracks.OrderBy(i => i.ParentIndexNumber ?? 0).ThenBy(i => i.IndexNumber ?? 0))
            {
                var trackNfo = new TrackNfo()
                {
                    Title = track.Name,
                    Positoin = track.IndexNumber
                };

                if (track.RunTimeTicks.HasValue)
                {
                    trackNfo.Duration = TimeSpan.FromTicks(track.RunTimeTicks.Value).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
                }

                trackNfos.Add(trackNfo);
            }

            nfo.Tracks = trackNfos.ToArray();

            base.MapJellyfinToNfoObject(item, nfo);
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
            => AlbumNfoProvider.GetAlbumSavePath(new ItemInfo(item));

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is MusicAlbum && updateType >= MinimumUpdateType;
    }
}
