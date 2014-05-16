using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localization;

        public SubtitleResolver(ILocalizationManager localization)
        {
            _localization = localization;
        }

        public IEnumerable<MediaStream> GetExternalSubtitleStreams(Video video,
          int startIndex,
          IDirectoryService directoryService,
          bool clearCache)
        {
            var files = GetSubtitleFiles(video, directoryService, clearCache);

            var streams = new List<MediaStream>();

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            foreach (var file in files)
            {
                var fullName = file.FullName;

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);

                var codec = Path.GetExtension(fullName).ToLower().TrimStart('.');

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

                    // Support xbmc naming conventions - 300.spanish.srt
                    var language = fileNameWithoutExtension
                        .Replace(".forced", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Replace(".foreign", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Split('.')
                        .LastOrDefault();

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var culture = _localization.GetCultures()
                        .FirstOrDefault(i => string.Equals(i.DisplayName, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.ThreeLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));

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
                        IsForced = isForced
                    });
                }
            }

            return streams;
        }

        private static IEnumerable<string> SubtitleExtensions
        {
            get
            {
                return new[] { ".srt", ".ssa", ".ass", ".sub" };
            }
        }

        public static IEnumerable<FileSystemInfo> GetSubtitleFiles(Video video, IDirectoryService directoryService, bool clearCache)
        {
            var containingPath = video.ContainingFolderPath;

            if (string.IsNullOrEmpty(containingPath))
            {
                throw new ArgumentException(string.Format("Cannot search for items that don't have a path: {0} {1}", video.Name, video.Id));
            }

            var files = directoryService.GetFiles(containingPath, clearCache);

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            return files.Where(i =>
            {
                if (!i.Attributes.HasFlag(FileAttributes.Directory) &&
                    SubtitleExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    var fullName = i.FullName;

                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);

                    if (string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            });
        }
    }
}
