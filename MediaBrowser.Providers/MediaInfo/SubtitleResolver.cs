using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Emby.Naming.Subtitles;
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
    /// Resolves external subtitles for videos.
    /// </summary>
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly NamingOptions _namingOptions;
        private readonly SubtitleFilePathParser _subtitleFilePathParser;
        private readonly CompareInfo _compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        private const CompareOptions CompareOptions = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace | System.Globalization.CompareOptions.IgnoreSymbols;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleResolver"/> class.
        /// </summary>
        /// <param name="localization">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="namingOptions">The naming Options.</param>
        public SubtitleResolver(
            ILocalizationManager localization,
            IMediaEncoder mediaEncoder,
            NamingOptions namingOptions)
        {
            _localizationManager = localization;
            _mediaEncoder = mediaEncoder;
            _namingOptions = namingOptions;
            _subtitleFilePathParser = new SubtitleFilePathParser(_namingOptions);
        }

        /// <summary>
        /// Retrieves the external subtitle streams for the provided video.
        /// </summary>
        /// <param name="video">The video to search from.</param>
        /// <param name="startIndex">The stream index to start adding subtitle streams at.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The external subtitle streams located.</returns>
        public async IAsyncEnumerable<MediaStream> GetExternalSubtitleStreams(
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

            var subtitleFileInfos = GetExternalSubtitleFiles(video, directoryService, clearCache);

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            foreach (var subtitleFileInfo in subtitleFileInfos)
            {
                string fileName = Path.GetFileName(subtitleFileInfo.Path);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(subtitleFileInfo.Path);
                Model.MediaInfo.MediaInfo mediaInfo = await GetMediaInfo(subtitleFileInfo.Path, cancellationToken).ConfigureAwait(false);

                if (mediaInfo.MediaStreams.Count == 1)
                {
                    MediaStream mediaStream = mediaInfo.MediaStreams.First();
                    mediaStream.Index = startIndex++;
                    mediaStream.Type = MediaStreamType.Subtitle;
                    mediaStream.IsExternal = true;
                    mediaStream.Path = subtitleFileInfo.Path;
                    mediaStream.IsDefault = subtitleFileInfo.IsDefault || mediaStream.IsDefault;
                    mediaStream.IsForced = subtitleFileInfo.IsForced || mediaStream.IsForced;

                    yield return DetectLanguage(mediaStream, fileNameWithoutExtension, videoFileNameWithoutExtension);
                }
                else
                {
                    foreach (MediaStream mediaStream in mediaInfo.MediaStreams)
                    {
                        mediaStream.Index = startIndex++;
                        mediaStream.Type = MediaStreamType.Subtitle;
                        mediaStream.IsExternal = true;
                        mediaStream.Path = subtitleFileInfo.Path;

                        yield return DetectLanguage(mediaStream, fileNameWithoutExtension, videoFileNameWithoutExtension);
                    }
                }
            }
        }

        /// <summary>
        /// Locates the external subtitle files for the provided video.
        /// </summary>
        /// <param name="video">The video to search from.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external subtitle file paths located.</returns>
        public IEnumerable<SubtitleFileInfo> GetExternalSubtitleFiles(
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

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            var files = directoryService.GetFilePaths(folder, clearCache, true);
            for (int i = 0; i < files.Count; i++)
            {
                var subtitleFileInfo = _subtitleFilePathParser.ParseFile(files[i]);

                if (subtitleFileInfo == null)
                {
                    continue;
                }

                yield return subtitleFileInfo;
            }
        }

        /// <summary>
        /// Returns the media info of the given subtitle file.
        /// </summary>
        /// <param name="path">The path to the subtitle file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The media info for the given subtitle file.</returns>
        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = DlnaProfileType.Subtitle,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File
                    }
                },
                cancellationToken);
        }

        private MediaStream DetectLanguage(MediaStream mediaStream, string fileNameWithoutExtension, string videoFileNameWithoutExtension)
        {
            // Support xbmc naming conventions - 300.spanish.srt
            var languageString = fileNameWithoutExtension;
            while (languageString.Length > 0)
            {
                var lastDot = languageString.LastIndexOf('.');
                if (lastDot < videoFileNameWithoutExtension.Length)
                {
                    break;
                }

                var currentSlice = languageString[lastDot..];
                languageString = languageString[..lastDot];

                if (currentSlice.Equals(".default", StringComparison.OrdinalIgnoreCase)
                    || currentSlice.Equals(".forced", StringComparison.OrdinalIgnoreCase)
                    || currentSlice.Equals(".foreign", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var currentSliceString = currentSlice[1..];

                // Try to translate to three character code
                var culture = _localizationManager.FindLanguageInfo(currentSliceString);

                if (culture == null || mediaStream.Language != null)
                {
                    if (mediaStream.Title == null)
                    {
                        mediaStream.Title = currentSliceString;
                    }
                }
                else
                {
                    mediaStream.Language = culture.ThreeLetterISOLanguageName;
                }
            }

            return mediaStream;
        }
    }
}
