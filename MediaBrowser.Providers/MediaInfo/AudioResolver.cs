#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILocalizationManager _localization;

        private readonly IMediaEncoder _mediaEncoder;

        private readonly CancellationToken _cancellationToken;

        public AudioResolver(ILocalizationManager localization, IMediaEncoder mediaEncoder, CancellationToken cancellationToken = default)
        {
            _localization = localization;
            _mediaEncoder = mediaEncoder;
            _cancellationToken = cancellationToken;
        }

        public List<MediaStream> GetExternalAudioStreams(
            Video video,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            var streams = new List<MediaStream>();

            if (!video.IsFileProtocol)
            {
                return streams;
            }

            AddExternalAudioStreams(streams, video.ContainingFolderPath, video.Path, startIndex, directoryService, clearCache);

            startIndex += streams.Count;

            string folder = video.GetInternalMetadataPath();

            if (!Directory.Exists(folder))
            {
                return streams;
            }

            try
            {
                AddExternalAudioStreams(streams, folder, video.Path, startIndex, directoryService, clearCache);
            }
            catch (IOException)
            {
            }

            return streams;
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

            var streams = GetExternalAudioStreams(video, 0, directoryService, clearCache);

            foreach (var stream in streams)
            {
                yield return stream.Path;
            }
        }

        public void AddExternalAudioStreams(
            List<MediaStream> streams,
            string videoPath,
            int startIndex,
            IReadOnlyList<string> files)
        {
            var videoFileNameWithoutExtension = NormalizeFilenameForAudioComparison(videoPath);

            for (var i = 0; i < files.Count; i++)
            {

                var fullName = files[i];
                var extension = Path.GetExtension(fullName.AsSpan());
                if (!IsAudioExtension(extension))
                {
                    continue;
                }

                Model.MediaInfo.MediaInfo mediaInfo = GetMediaInfo(fullName).Result;
                MediaStream mediaStream = mediaInfo.MediaStreams.First();
                mediaStream.Index = startIndex++;
                mediaStream.Type = MediaStreamType.Audio;
                mediaStream.IsExternal = true;
                mediaStream.Path = fullName;
                mediaStream.IsDefault = false;
                mediaStream.Title = null;

                var fileNameWithoutExtension = NormalizeFilenameForAudioComparison(fullName);

                // The audio filename must either be equal to the video filename or start with the video filename followed by a dot
                if (videoFileNameWithoutExtension.Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    mediaStream.Path = fullName;
                }
                else if (fileNameWithoutExtension.Length > videoFileNameWithoutExtension.Length
                         && fileNameWithoutExtension[videoFileNameWithoutExtension.Length] == '.'
                         && fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {

                    // Support xbmc naming conventions - 300.spanish.m4a
                    var languageSpan = fileNameWithoutExtension;
                    while (languageSpan.Length > 0)
                    {
                        var lastDot = languageSpan.LastIndexOf('.');
                        var currentSlice = languageSpan[lastDot..];
                        languageSpan = languageSpan[(lastDot + 1)..];
                        break;
                    }

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var language = languageSpan.ToString();
                    var culture = _localization.FindLanguageInfo(language);

                    language = culture == null ? language : culture.ThreeLetterISOLanguageName;
                    mediaStream.Language = language;
                }
                else
                {
                    continue;
                }

                mediaStream.Codec = extension.TrimStart('.').ToString().ToLowerInvariant();

                streams.Add(mediaStream);
            }
        }

        private static bool IsAudioExtension(ReadOnlySpan<char> extension)
        {
            String[] audioExtensions = new[]
            {
                ".nsv",
                ".m4a",
                ".flac",
                ".aac",
                ".strm",
                ".pls",
                ".rm",
                ".mpa",
                ".wav",
                ".wma",
                ".ogg",
                ".opus",
                ".mp3",
                ".mp2",
                ".mod",
                ".amf",
                ".669",
                ".dmf",
                ".dsm",
                ".far",
                ".gdm",
                ".imf",
                ".it",
                ".m15",
                ".med",
                ".okt",
                ".s3m",
                ".stm",
                ".sfx",
                ".ult",
                ".uni",
                ".xm",
                ".sid",
                ".ac3",
                ".dts",
                ".cue",
                ".aif",
                ".aiff",
                ".ape",
                ".mac",
                ".mpc",
                ".mp+",
                ".mpp",
                ".shn",
                ".wv",
                ".nsf",
                ".spc",
                ".gym",
                ".adplug",
                ".adx",
                ".dsp",
                ".adp",
                ".ymf",
                ".ast",
                ".afc",
                ".hps",
                ".xsp",
                ".acc",
                ".m4b",
                ".oga",
                ".dsf",
                ".mka"
            };

            foreach (String audioExtension in audioExtensions)
            {
                if (extension.Equals(audioExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path)
        {
            _cancellationToken.ThrowIfCancellationRequested();

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
                _cancellationToken);
        }

        private static ReadOnlySpan<char> NormalizeFilenameForAudioComparison(string filename)
        {
            // Try to account for sloppy file naming
            filename = filename.Replace("_", string.Empty, StringComparison.Ordinal);
            filename = filename.Replace(" ", string.Empty, StringComparison.Ordinal);
            return Path.GetFileNameWithoutExtension(filename.AsSpan());
        }

        private void AddExternalAudioStreams(
            List<MediaStream> streams,
            string folder,
            string videoPath,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            var files = directoryService.GetFilePaths(folder, clearCache, true);

            AddExternalAudioStreams(streams, videoPath, startIndex, files);
        }
    }
}
