#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localization;

        public SubtitleResolver(ILocalizationManager localization)
        {
            _localization = localization;
        }

        public List<MediaStream> GetExternalSubtitleStreams(
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

            AddExternalSubtitleStreams(streams, video.ContainingFolderPath, video.Path, startIndex, directoryService, clearCache);

            startIndex += streams.Count;

            string folder = video.GetInternalMetadataPath();

            if (!Directory.Exists(folder))
            {
                return streams;
            }

            try
            {
                AddExternalSubtitleStreams(streams, folder, video.Path, startIndex, directoryService, clearCache);
            }
            catch (IOException)
            {
            }

            return streams;
        }

        public IEnumerable<string> GetExternalSubtitleFiles(
            Video video,
            IDirectoryService directoryService,
            bool clearCache)
        {
            if (!video.IsFileProtocol)
            {
                yield break;
            }

            var streams = GetExternalSubtitleStreams(video, 0, directoryService, clearCache);

            foreach (var stream in streams)
            {
                yield return stream.Path;
            }
        }

        public void AddExternalSubtitleStreams(
            List<MediaStream> streams,
            string videoPath,
            int startIndex,
            IReadOnlyList<string> files)
        {
            var videoFileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(videoPath);

            for (var i = 0; i < files.Count; i++)
            {
                var fullName = files[i];
                var extension = Path.GetExtension(fullName.AsSpan());
                if (!IsSubtitleExtension(extension))
                {
                    continue;
                }

                var fileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(fullName);

                MediaStream mediaStream;

                // The subtitle filename must either be equal to the video filename or start with the video filename followed by a dot
                if (videoFileNameWithoutExtension.Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    mediaStream = new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName
                    };
                }
                else if (fileNameWithoutExtension.Length > videoFileNameWithoutExtension.Length
                         && fileNameWithoutExtension[videoFileNameWithoutExtension.Length] == '.'
                         && fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var isForced = fullName.Contains(".forced.", StringComparison.OrdinalIgnoreCase)
                                   || fullName.Contains(".foreign.", StringComparison.OrdinalIgnoreCase);

                    var isDefault = fullName.Contains(".default.", StringComparison.OrdinalIgnoreCase);

                    // Support xbmc naming conventions - 300.spanish.srt
                    var languageSpan = fileNameWithoutExtension;
                    while (languageSpan.Length > 0)
                    {
                        var lastDot = languageSpan.LastIndexOf('.');
                        var currentSlice = languageSpan[lastDot..];
                        if (currentSlice.Equals(".default", StringComparison.OrdinalIgnoreCase)
                            || currentSlice.Equals(".forced", StringComparison.OrdinalIgnoreCase)
                            || currentSlice.Equals(".foreign", StringComparison.OrdinalIgnoreCase))
                        {
                            languageSpan = languageSpan[..lastDot];
                            continue;
                        }

                        languageSpan = languageSpan[(lastDot + 1)..];
                        break;
                    }

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var language = languageSpan.ToString();
                    var culture = _localization.FindLanguageInfo(language);

                    language = culture == null ? language : culture.ThreeLetterISOLanguageName;

                    mediaStream = new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName,
                        Language = language,
                        IsForced = isForced,
                        IsDefault = isDefault
                    };
                }
                else
                {
                    continue;
                }

                mediaStream.Codec = extension.TrimStart('.').ToString().ToLowerInvariant();

                streams.Add(mediaStream);
            }
        }

        private static bool IsSubtitleExtension(ReadOnlySpan<char> extension)
        {
            return extension.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".ssa", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".sub", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".smi", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".sami", StringComparison.OrdinalIgnoreCase);
        }

        private static ReadOnlySpan<char> NormalizeFilenameForSubtitleComparison(string filename)
        {
            // Try to account for sloppy file naming
            filename = filename.Replace("_", string.Empty, StringComparison.Ordinal);
            filename = filename.Replace(" ", string.Empty, StringComparison.Ordinal);
            return Path.GetFileNameWithoutExtension(filename.AsSpan());
        }

        private void AddExternalSubtitleStreams(
            List<MediaStream> streams,
            string folder,
            string videoPath,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            var files = directoryService.GetFilePaths(folder, clearCache, true);

            AddExternalSubtitleStreams(streams, videoPath, startIndex, files);
        }
    }
}
