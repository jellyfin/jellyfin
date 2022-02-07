using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Emby.Naming.ExternalFiles;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Resolves external files for videos.
    /// </summary>
    public class MediaInfoResolver
    {
        /// <summary>
        /// The <see cref="CompareOptions"/> instance.
        /// </summary>
        private const CompareOptions CompareOptions = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace | System.Globalization.CompareOptions.IgnoreSymbols;

        /// <summary>
        /// The <see cref="CompareInfo"/> instance.
        /// </summary>
        private readonly CompareInfo _compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        /// <summary>
        /// The <see cref="ExternalPathParser"/> instance.
        /// </summary>
        private readonly ExternalPathParser _externalPathParser;

        /// <summary>
        /// The <see cref="IMediaEncoder"/> instance.
        /// </summary>
        private readonly IMediaEncoder _mediaEncoder;

        /// <summary>
        /// The <see cref="DlnaProfileType"/> of the files this resolver should resolve.
        /// </summary>
        private readonly DlnaProfileType _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoResolver"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/> of the parsed file.</param>
        public MediaInfoResolver(
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            NamingOptions namingOptions,
            DlnaProfileType type)
        {
            _mediaEncoder = mediaEncoder;
            _type = type;
            _externalPathParser = new ExternalPathParser(namingOptions, localizationManager, _type);
        }

        /// <summary>
        /// Retrieves the external streams for the provided video.
        /// </summary>
        /// <param name="video">The <see cref="Video"/> object to search external streams for.</param>
        /// <param name="startIndex">The stream index to start adding external streams at.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The external streams located.</returns>
        public async IAsyncEnumerable<MediaStream> GetExternalStreamsAsync(
            Video video,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!video.IsFileProtocol)
            {
                yield break;
            }

            var pathInfos = GetExternalFiles(video, directoryService, clearCache);

            foreach (var pathInfo in pathInfos)
            {
                Model.MediaInfo.MediaInfo mediaInfo = await GetMediaInfo(pathInfo.Path, _type, cancellationToken).ConfigureAwait(false);

                if (mediaInfo.MediaStreams.Count == 1)
                {
                    MediaStream mediaStream = mediaInfo.MediaStreams.First();
                    mediaStream.Index = startIndex++;
                    mediaStream.IsDefault = pathInfo.IsDefault || mediaStream.IsDefault;
                    mediaStream.IsForced = pathInfo.IsForced || mediaStream.IsForced;

                    yield return MergeMetadata(mediaStream, pathInfo);
                }
                else
                {
                    foreach (MediaStream mediaStream in mediaInfo.MediaStreams)
                    {
                        mediaStream.Index = startIndex++;

                        yield return MergeMetadata(mediaStream, pathInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the external file infos for the given video.
        /// </summary>
        /// <param name="video">The <see cref="Video"/> object to search external files for.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external file paths located.</returns>
        public IEnumerable<ExternalPathParserResult> GetExternalFiles(
            Video video,
            IDirectoryService directoryService,
            bool clearCache)
        {
            if (!video.IsFileProtocol)
            {
                yield break;
            }

            // Check if video folder exists
            string folder = video.ContainingFolderPath;
            if (!Directory.Exists(folder))
            {
                yield break;
            }

            var files = directoryService.GetFilePaths(folder, clearCache).ToList();
            files.AddRange(directoryService.GetFilePaths(video.GetInternalMetadataPath(), clearCache));

            foreach (var file in files)
            {
                if (_compareInfo.IsPrefix(Path.GetFileNameWithoutExtension(file), video.FileNameWithoutExtension, CompareOptions, out int matchLength))
                {
                    var externalPathInfo = _externalPathParser.ParseFile(file, Path.GetFileNameWithoutExtension(file)[matchLength..]);

                    if (externalPathInfo != null)
                    {
                        yield return externalPathInfo;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the media info of the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The media info for the given file.</returns>
        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path, DlnaProfileType type, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = type,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File
                    }
                },
                cancellationToken);
        }

        /// <summary>
        /// Merges path metadata into stream metadata.
        /// </summary>
        /// <param name="mediaStream">The <see cref="MediaStream"/> object.</param>
        /// <param name="pathInfo">The <see cref="ExternalPathParserResult"/> object.</param>
        /// <returns>The modified mediaStream.</returns>
        private MediaStream MergeMetadata(MediaStream mediaStream, ExternalPathParserResult pathInfo)
        {
            mediaStream.Path = pathInfo.Path;
            mediaStream.IsExternal = true;
            mediaStream.Title = string.IsNullOrEmpty(mediaStream.Title) ? (string.IsNullOrEmpty(pathInfo.Title) ? null : pathInfo.Title) : mediaStream.Title;
            mediaStream.Language = string.IsNullOrEmpty(mediaStream.Language) ? (string.IsNullOrEmpty(pathInfo.Language) ? null : pathInfo.Language) : mediaStream.Language;

            mediaStream.Type = _type switch
            {
                DlnaProfileType.Audio => MediaStreamType.Audio,
                DlnaProfileType.Subtitle => MediaStreamType.Subtitle,
                DlnaProfileType.Video => MediaStreamType.Video,
                _ => mediaStream.Type
            };

            return mediaStream;
        }
    }
}
