using System.IO;
using Jellyfin.NfoMetadata.Models;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.NfoMetadata.Providers
{
    /// <summary>
    /// The music artist nfo metadata provider.
    /// </summary>
    public class ArtistNfoProvider : BaseNfoProvider<MusicArtist, ArtistNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public ArtistNfoProvider(ILogger<BaseNfoProvider<MusicArtist, ArtistNfo>> logger, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        internal static string GetArtistSavePath(ItemInfo info)
            => Path.Combine(info.Path, "artist.nfo");

        /// <inheritdoc />
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(GetArtistSavePath(info));
    }
}
