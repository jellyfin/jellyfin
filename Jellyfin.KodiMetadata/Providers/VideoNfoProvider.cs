﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// Video nfo metadata provider (Movie and music video).
    /// </summary>
    public class VideoNfoProvider : BaseNfoProvider<Video, VideoNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public VideoNfoProvider(
            ILogger<BaseNfoProvider<Video, VideoNfo>> logger,
            IFileSystem fileSystem,
            IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(VideoNfo? nfo, MetadataResult<Video> metadataResult)
        {
            if (nfo == null)
            {
                return;
            }

            base.MapNfoToJellyfinObject(nfo, metadataResult);

            if (nfo.FileInfo?.StreamDetails?.Video != null)
            {
                foreach (var videoNfo in nfo.FileInfo.StreamDetails.Video)
                {
                    switch (videoNfo.Format3D)
                    {
                        case "HSBS":
                            metadataResult.Item.Video3DFormat = Video3DFormat.HalfSideBySide;
                            break;
                        case "HTAG":
                            metadataResult.Item.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                            break;
                        case "FTAB":
                            metadataResult.Item.Video3DFormat = Video3DFormat.FullTopAndBottom;
                            break;
                        case "FSBS":
                            metadataResult.Item.Video3DFormat = Video3DFormat.FullSideBySide;
                            break;
                        case "MVC":
                            metadataResult.Item.Video3DFormat = Video3DFormat.MVC;
                            break;
                    }
                }
            }

            // handle sets
            if (metadataResult.Item is Movie movie)
            {
                movie.SetProviderId(MetadataProvider.TmdbCollection, nfo.Set?.TmdbCollectionId!);
                movie.CollectionName = nfo.Set.Name;
            }

            if (metadataResult.Item is MusicVideo musicVideo)
            {
                musicVideo.Album = nfo.Album;
                musicVideo.Artists = nfo.Artists;
            }
        }

        /// <inheritdoc />
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return GetMovieSavePaths(info)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i != null);
        }

        internal static IEnumerable<string> GetMovieSavePaths(ItemInfo item)
        {
            if (item.VideoType == VideoType.Dvd && !item.IsPlaceHolder)
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, "VIDEO_TS", "VIDEO_TS.nfo");
            }

            if (!item.IsPlaceHolder && (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay))
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, Path.GetFileName(path) + ".nfo");
            }
            else
            {
                yield return Path.ChangeExtension(item.Path, ".nfo");

                if (!item.IsInMixedFolder)
                {
                    yield return Path.Combine(item.ContainingFolderPath, "movie.nfo");
                }
            }
        }
    }
}
