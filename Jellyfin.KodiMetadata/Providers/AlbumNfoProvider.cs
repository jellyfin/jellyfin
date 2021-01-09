using System.IO;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// The music album nfo metadata provider.
    /// </summary>
    public class AlbumNfoProvider : BaseNfoProvider<MusicAlbum, BaseNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public AlbumNfoProvider(ILogger<BaseNfoProvider<MusicAlbum, BaseNfo>> logger, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc />
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "album.nfo"));
    }
}
