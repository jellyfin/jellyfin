using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.NfoMetadata.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.NfoMetadata.Providers
{
    /// <summary>
    /// Music video nfo metadata provider.
    /// </summary>
    public class MusicVideoNfoProvider : BaseNfoProvider<MusicVideo, MusicVideoNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicVideoNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public MusicVideoNfoProvider(
            ILogger<BaseNfoProvider<MusicVideo, MusicVideoNfo>> logger,
            IFileSystem fileSystem,
            IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(MusicVideoNfo? nfo, MetadataResult<MusicVideo> metadataResult)
        {
            if (nfo == null)
            {
                throw new ArgumentException("Nfo can't be null", nameof(nfo));
            }

            base.MapNfoToJellyfinObject(nfo, metadataResult);

            var item = metadataResult.Item;

            item.Album = nfo.Album;
            item.Artists = nfo.Artists;
        }

        /// <inheritdoc />
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return GetMusicVideoSavePaths(info)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i != null);
        }

        internal static IEnumerable<string> GetMusicVideoSavePaths(ItemInfo item)
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
