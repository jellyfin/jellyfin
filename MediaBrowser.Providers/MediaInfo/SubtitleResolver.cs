using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localization;

        private static readonly HashSet<string> SubtitleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".srt",
            ".ssa",
            ".ass",
            ".sub",
            ".smi",
            ".sami",
            ".vtt"
        };

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

        public List<string> GetExternalSubtitleFiles(Video video,
          IDirectoryService directoryService,
          bool clearCache)
        {
            var list = new List<string>();

            if (!video.IsFileProtocol)
            {
                return list;
            }

            var streams = GetExternalSubtitleStreams(video, 0, directoryService, clearCache);

            foreach (var stream in streams)
            {
                list.Add(stream.Path);
            }

            return list;
        }

        private void AddExternalSubtitleStreams(List<MediaStream> streams, string folder,
            string videoPath,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            var files = directoryService.GetFilePaths(folder, clearCache).OrderBy(i => i).ToArray();

            AddExternalSubtitleStreams(streams, videoPath, startIndex, files);
        }

        public void AddExternalSubtitleStreams(List<MediaStream> streams,
            string videoPath,
            int startIndex,
            string[] files)
        {
            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoPath);
            videoFileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(videoFileNameWithoutExtension);

            foreach (var fullName in files)
            {
                var extension = Path.GetExtension(fullName);

                if (!SubtitleExtensions.Contains(extension))
                {
                    continue;
                }

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
                fileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(fileNameWithoutExtension);

                if (!string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                    !fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var codec = Path.GetExtension(fullName).ToLowerInvariant().TrimStart('.');

                if (string.Equals(codec, "txt", StringComparison.OrdinalIgnoreCase))
                {
                    codec = "srt";
                }

                // If the subtitle file matches the video file name
                if (string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    streams.Add(new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName,
                        Codec = codec
                    });
                }
                else if (fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                {
                    var isForced = fullName.IndexOf(".forced.", StringComparison.OrdinalIgnoreCase) != -1 ||
                        fullName.IndexOf(".foreign.", StringComparison.OrdinalIgnoreCase) != -1;

                    var isDefault = fullName.IndexOf(".default.", StringComparison.OrdinalIgnoreCase) != -1;

                    // Support xbmc naming conventions - 300.spanish.srt
                    var language = fileNameWithoutExtension
                        .Replace(".forced", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Replace(".foreign", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Replace(".default", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Split('.')
                        .LastOrDefault();

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var culture = _localization.FindLanguageInfo(language);

                    if (culture != null)
                    {
                        language = culture.ThreeLetterISOLanguageName;
                    }

                    streams.Add(new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName,
                        Codec = codec,
                        Language = language,
                        IsForced = isForced,
                        IsDefault = isDefault
                    });
                }
            }
        }

        private string NormalizeFilenameForSubtitleComparison(string filename)
        {
            // Try to account for sloppy file naming
            filename = filename.Replace("_", string.Empty);
            filename = filename.Replace(" ", string.Empty);

            // can't normalize this due to languages such as pt-br
            //filename = filename.Replace("-", string.Empty);

            //filename = filename.Replace(".", string.Empty);

            return filename;
        }
    }
}
