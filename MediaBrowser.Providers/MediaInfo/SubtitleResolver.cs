using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleResolver
    {
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;

        public SubtitleResolver(ILocalizationManager localization, IFileSystem fileSystem)
        {
            _localization = localization;
            _fileSystem = fileSystem;
        }

        public IEnumerable<MediaStream> GetExternalSubtitleStreams(Video video,
          int startIndex,
          IDirectoryService directoryService,
          bool clearCache)
        {
            var files = GetSubtitleFiles(video, directoryService, _fileSystem, clearCache);

            var streams = new List<MediaStream>();

            var videoFileNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(video.Path);
            videoFileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(videoFileNameWithoutExtension);

            foreach (var file in files)
            {
                var fullName = file.FullName;

                var fileNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(file);
                fileNameWithoutExtension = NormalizeFilenameForSubtitleComparison(fileNameWithoutExtension);

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

            return streams;
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

        private static IEnumerable<string> SubtitleExtensions
        {
            get
            {
                return new[] { ".srt", ".ssa", ".ass", ".sub" };
            }
        }

        public static IEnumerable<FileSystemMetadata> GetSubtitleFiles(Video video, IDirectoryService directoryService, IFileSystem fileSystem, bool clearCache)
        {
            var containingPath = video.ContainingFolderPath;

            if (string.IsNullOrEmpty(containingPath))
            {
                throw new ArgumentException(string.Format("Cannot search for items that don't have a path: {0} {1}", video.Name, video.Id));
            }

            var files = directoryService.GetFiles(containingPath, clearCache);

            var videoFileNameWithoutExtension = fileSystem.GetFileNameWithoutExtension(video.Path);

            return files.Where(i =>
            {
                if (!i.Attributes.HasFlag(FileAttributes.Directory) &&
                    SubtitleExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    var fileNameWithoutExtension = fileSystem.GetFileNameWithoutExtension(i);

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
