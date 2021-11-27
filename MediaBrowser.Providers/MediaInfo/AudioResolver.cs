#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task<List<MediaStream>> GetExternalAudioStreams(
            Video video,
            int startIndex,
            IDirectoryService directoryService,
            NamingOptions namingOptions,
            bool clearCache,
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            CancellationToken cancellationToken)
        {
            var streams = new List<MediaStream>();

            if (!video.IsFileProtocol)
            {
                return streams;
            }

            List<string> paths = GetExternalAudioFiles(video, directoryService, namingOptions, clearCache);

            await AddExternalAudioStreams(streams, paths, startIndex, localizationManager, mediaEncoder, cancellationToken).ConfigureAwait(false);

            return streams;
        }

        public List<string> GetExternalAudioFiles(
            Video video,
            IDirectoryService directoryService,
            NamingOptions namingOptions,
            bool clearCache)
        {
            List<string> paths = new List<string>();

            if (!video.IsFileProtocol)
            {
                return paths;
            }

            paths.AddRange(GetAudioFilesFromFolder(video.ContainingFolderPath, video.Path, directoryService, namingOptions, clearCache));
            paths.AddRange(GetAudioFilesFromFolder(video.GetInternalMetadataPath(), video.Path, directoryService, namingOptions, clearCache));

            return paths;
        }

        private List<string> GetAudioFilesFromFolder(
            string folder,
            string videoFileName,
            IDirectoryService directoryService,
            NamingOptions namingOptions,
            bool clearCache)
        {
            List<string> paths = new List<string>();
            string videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoFileName);

            if (!Directory.Exists(folder))
            {
                return paths;
            }

            var files = directoryService.GetFilePaths(folder, clearCache, true);
            for (int i = 0; i < files.Count; i++)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);

                if (!AudioFileParser.IsAudioFile(files[i], namingOptions))
                {
                    continue;
                }

                // The audio filename must either be equal to the video filename or start with the video filename followed by a dot
                if (videoFileNameWithoutExtension.Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) ||
                    (fileNameWithoutExtension.Length > videoFileNameWithoutExtension.Length
                         && fileNameWithoutExtension[videoFileNameWithoutExtension.Length] == '.'
                         && fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    paths.Add(files[i]);
                }
            }

            return paths;
        }

        public async Task AddExternalAudioStreams(
            List<MediaStream> streams,
            List<string> paths,
            int startIndex,
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            CancellationToken cancellationToken)
        {
            foreach (string path in paths)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                Model.MediaInfo.MediaInfo mediaInfo = await GetMediaInfo(path, mediaEncoder, cancellationToken);

                foreach (MediaStream mediaStream in mediaInfo.MediaStreams)
                {
                    mediaStream.Index = startIndex++;
                    mediaStream.Type = MediaStreamType.Audio;
                    mediaStream.IsExternal = true;
                    mediaStream.Path = path;
                    mediaStream.IsDefault = false;
                    mediaStream.Title = null;

                    if (mediaStream.Language == null)
                    {
                        // Try to translate to three character code
                        // Be flexible and check against both the full and three character versions
                        var language = StringExtensions.RightPart(fileNameWithoutExtension, '.').ToString();

                        if (language != fileNameWithoutExtension)
                        {
                            var culture = localizationManager.FindLanguageInfo(language);

                            language = culture == null ? language : culture.ThreeLetterISOLanguageName;
                            mediaStream.Language = language;
                        }
                    }

                    streams.Add(mediaStream);
                }
            }
        }

        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path, IMediaEncoder mediaEncoder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return mediaEncoder.GetMediaInfo(
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
