using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Jellyfin.Extensions;
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
    public class AudioResolver
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly NamingOptions _namingOptions;

        public AudioResolver(
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            NamingOptions namingOptions)
        {
            _localizationManager = localizationManager;
            _mediaEncoder = mediaEncoder;
            _namingOptions = namingOptions;
        }

        public async IAsyncEnumerable<MediaStream> GetExternalAudioStreams(
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

            IEnumerable<string> paths = GetExternalAudioFiles(video, directoryService, clearCache);
            foreach (string path in paths)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                Model.MediaInfo.MediaInfo mediaInfo = await GetMediaInfo(path, cancellationToken).ConfigureAwait(false);

                foreach (MediaStream mediaStream in mediaInfo.MediaStreams)
                {
                    mediaStream.Index = startIndex++;
                    mediaStream.Type = MediaStreamType.Audio;
                    mediaStream.IsExternal = true;
                    mediaStream.Path = path;
                    mediaStream.IsDefault = false;
                    mediaStream.Title = null;

                    if (string.IsNullOrEmpty(mediaStream.Language))
                    {
                        // Try to translate to three character code
                        // Be flexible and check against both the full and three character versions
                        var language = StringExtensions.RightPart(fileNameWithoutExtension, '.').ToString();

                        if (language != fileNameWithoutExtension)
                        {
                            var culture = _localizationManager.FindLanguageInfo(language);

                            language = culture == null ? language : culture.ThreeLetterISOLanguageName;
                            mediaStream.Language = language;
                        }
                    }

                    yield return mediaStream;
                }
            }
        }

        public IEnumerable<string> GetExternalAudioFiles(
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

            string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            var files = directoryService.GetFilePaths(folder, clearCache, true);
            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                if (!AudioFileParser.IsAudioFile(file, _namingOptions))
                {
                    continue;
                }

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                // The audio filename must either be equal to the video filename or start with the video filename followed by a dot
                if (videoFileNameWithoutExtension.Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase)
                    || (fileNameWithoutExtension.Length > videoFileNameWithoutExtension.Length
                        && fileNameWithoutExtension[videoFileNameWithoutExtension.Length] == '.'
                        && fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return file;
                }
            }
        }

        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = DlnaProfileType.Audio,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File
                    }
                },
                cancellationToken);
        }
    }
}
