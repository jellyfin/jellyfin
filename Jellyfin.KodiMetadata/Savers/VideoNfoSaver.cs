using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.KodiMetadata.Models;
using Jellyfin.KodiMetadata.Providers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// the video nfo metadata saver.
    /// </summary>
    public class VideoNfoSaver : BaseNfoSaver<Video, VideoNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public VideoNfoSaver(
            ILogger<BaseNfoSaver<Video, VideoNfo>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager)
            : base(logger, xmlSerializer, fileSystem, configurationManager)
        {
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            return VideoNfoProvider.GetMovieSavePaths(new ItemInfo(item)).FirstOrDefault() ?? Path.ChangeExtension(item.Path, ".nfo");
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
        protected override void MapJellyfinToNfoObject(BaseItem item, VideoNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            var imdbId = item.GetProviderId(MetadataProvider.Imdb);
            nfo.Id = imdbId;
            nfo.ImdbId = imdbId;

            if (item is MusicVideo musicVideo)
            {
                // Artists
                var artistList = new List<string>();
                foreach (var artist in musicVideo.Artists)
                {
                    artistList.Add(artist);
                }

                nfo.Artists = artistList.ToArray();

                // Album
                nfo.Album = musicVideo.Album;
            }

            // Collection
            if (item is Movie movie)
            {
                nfo.Set = new SetNfo() { Name = movie.CollectionName, TmdbCollectionId = movie.GetProviderId(MetadataProvider.TmdbCollection) };
            }

            base.MapJellyfinToNfoObject(item, nfo);
        }
    }
}
