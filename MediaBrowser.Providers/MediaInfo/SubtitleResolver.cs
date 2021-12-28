using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Resolves external subtitles for videos.
    /// </summary>
    public class SubtitleResolver
    {
        private const CompareOptions CompareOptions = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace | System.Globalization.CompareOptions.IgnoreSymbols;
        private readonly CompareInfo _compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleResolver"/> class.
        /// </summary>
        /// <param name="localization">The localization manager.</param>
        public SubtitleResolver(ILocalizationManager localization)
        {
            _localization = localization;
        }

        /// <summary>
        /// Retrieves the external subtitle streams for the provided video.
        /// </summary>
        /// <param name="video">The video to search from.</param>
        /// <param name="startIndex">The stream index to start adding subtitle streams at.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external subtitle streams located.</returns>
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

        /// <summary>
        /// Locates the external subtitle files for the provided video.
        /// </summary>
        /// <param name="video">The video to search from.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external subtitle file paths located.</returns>
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

        /// <summary>
        /// Extracts the subtitle files from the provided list and adds them to the list of streams.
        /// </summary>
        /// <param name="streams">The list of streams to add external subtitles to.</param>
        /// <param name="videoPath">The path to the video file.</param>
        /// <param name="startIndex">The stream index to start adding subtitle streams at.</param>
        /// <param name="files">The files to add if they are subtitles.</param>
        public void AddExternalSubtitleStreams(
            List<MediaStream> streams,
            string videoPath,
            int startIndex,
            IReadOnlyList<string> files)
        {
            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoPath.AsSpan());

            for (var i = 0; i < files.Count; i++)
            {
                var fullName = files[i];
                var extension = Path.GetExtension(fullName.AsSpan());
                if (!IsSubtitleExtension(extension))
                {
                    continue;
                }

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName.AsSpan());

                MediaStream mediaStream;

                // The subtitle filename must either be equal to the video filename or start with the video filename followed by a dot
                if (_compareInfo.Compare(videoFileNameWithoutExtension, fileNameWithoutExtension, CompareOptions) == 0)
                {
                    mediaStream = new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName
                    };
                }
                else if (_compareInfo.IsPrefix(fileNameWithoutExtension, videoFileNameWithoutExtension, CompareOptions, out int matchLength)
                         && fileNameWithoutExtension[matchLength] == '.')
                {
                    var isForced = fullName.Contains(".forced.", StringComparison.OrdinalIgnoreCase)
                                   || fullName.Contains(".foreign.", StringComparison.OrdinalIgnoreCase);

                    var isDefault = fullName.Contains(".default.", StringComparison.OrdinalIgnoreCase);

                    // Support xbmc naming conventions - 300.spanish.srt
                    var languageSpan = fileNameWithoutExtension;
                    while (languageSpan.Length > 0)
                    {
                        var lastDot = languageSpan.LastIndexOf('.');
                        if (lastDot < matchLength)
                        {
                            languageSpan = ReadOnlySpan<char>.Empty;
                            break;
                        }

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

                    var language = languageSpan.ToString();
                    if (string.IsNullOrWhiteSpace(language))
                    {
                        language = null;
                    }
                    else
                    {
                        // Try to translate to three character code
                        // Be flexible and check against both the full and three character versions
                        var culture = _localization.FindLanguageInfo(language);

                        language = culture == null ? language : culture.ThreeLetterISOLanguageName;
                    }

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
