using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;

        private string[] SubtitleExtensions = new[]
        {
            ".srt",
            ".ssa",
            ".ass",
            ".sub",
            ".smi",
            ".sami",
            ".vtt"
        };

        public SubtitleResolver(ILocalizationManager localization, IFileSystem fileSystem)
        {
            _localization = localization;
            _fileSystem = fileSystem;
        }

        public List<MediaStream> GetExternalSubtitleStreams(Video video,
          int startIndex,
          IDirectoryService directoryService,
          bool clearCache)
        {
            var streams = new List<MediaStream>();

            GetExternalSubtitleStreams(streams, video.ContainingFolderPath, video.Path, startIndex, directoryService, clearCache);

            startIndex += streams.Count;

            try
            {
                GetExternalSubtitleStreams(streams, video.GetInternalMetadataPath(), video.Path, startIndex, directoryService, clearCache);
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
            var streams = GetExternalSubtitleStreams(video, 0, directoryService, clearCache);

            var list = new List<string>();

            foreach (var stream in streams)
            {
                list.Add(stream.Path);
            }

            return list;
        }

        private void GetExternalSubtitleStreams(List<MediaStream> streams, string folder,
            string videoPath,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            var videoFileNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(videoPath);
            videoFileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(videoFileNameWithoutExtension);

            var files = directoryService.GetFilePaths(folder, clearCache);

            foreach (var fullName in files)
            {
                var extension = Path.GetExtension(fullName);

                if (!SubtitleExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(fullName);
                fileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(fileNameWithoutExtension);

                if (!string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                    !fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var codec = Path.GetExtension(fullName).ToLower().TrimStart('.');

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
                        .Split('.')
                        .LastOrDefault();

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var culture = _localization.GetCultures()
                        .FirstOrDefault(i => string.Equals(i.DisplayName, language, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(i.Name, language, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(i.ThreeLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(i.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));

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
