using System;
using System.Collections.Generic;
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
    /// The nfo local metadata saver for music artists.
    /// </summary>
    public class ArtistNfoSaver : BaseNfoSaver<MusicArtist, ArtistNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public ArtistNfoSaver(
            ILogger<BaseNfoSaver<MusicArtist, ArtistNfo>> logger,
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
        protected override void MapJellyfinToNfoObject(MusicArtist item, ArtistNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            nfo.Disbanded = item.EndDate;
            var albums = item
                .GetRecursiveChildren(i => i is MusicAlbum);

            List<ArtistAlbumNfo> albumNfos = new List<ArtistAlbumNfo>();
            foreach (var album in albums)
            {
                albumNfos.Add(new ArtistAlbumNfo()
                {
                    Name = album.Name,
                    Year = album.ProductionYear
                });
            }

            nfo.Albums = albumNfos.ToArray();

            base.MapJellyfinToNfoObject(item, nfo);
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
            => ArtistNfoProvider.GetArtistSavePath(new ItemInfo(item));

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is MusicArtist && updateType >= MinimumUpdateType;
    }
}
