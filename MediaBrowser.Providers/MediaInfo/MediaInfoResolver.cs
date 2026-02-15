using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Emby.Naming.ExternalFiles;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Resolves external files for <see cref="Video"/>.
    /// </summary>
    public abstract class MediaInfoResolver
    {
        /// <summary>
        /// The <see cref="ExternalPathParser"/> instance.
        /// </summary>
        private readonly ExternalPathParser _externalPathParser;

        /// <summary>
        /// The <see cref="IMediaEncoder"/> instance.
        /// </summary>
        private readonly IMediaEncoder _mediaEncoder;

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// The <see cref="NamingOptions"/> instance.
        /// </summary>
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// The <see cref="DlnaProfileType"/> of the files this resolver should resolve.
        /// </summary>
        private readonly DlnaProfileType _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/> of the parsed file.</param>
        protected MediaInfoResolver(
            ILogger logger,
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            NamingOptions namingOptions,
            DlnaProfileType type)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _namingOptions = namingOptions;
            _type = type;
            _externalPathParser = new ExternalPathParser(namingOptions, localizationManager, _type);
        }

        /// <summary>
        /// Retrieves the external streams for the provided video.
        /// </summary>
        /// <param name="video">The <see cref="Video"/> object to search external streams for.</param>
        /// <param name="startIndex">The stream index to start adding external streams at.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The external streams located.</returns>
        public async Task<IReadOnlyList<MediaStream>> GetExternalStreamsAsync(
            Video video,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache,
            CancellationToken cancellationToken)
        {
            if (!video.IsFileProtocol)
            {
                return Array.Empty<MediaStream>();
            }

            var pathInfos = GetExternalFiles(video, directoryService, clearCache);

            if (!pathInfos.Any())
            {
                return Array.Empty<MediaStream>();
            }

            var mediaStreams = new List<MediaStream>();

            foreach (var pathInfo in pathInfos)
            {
                if (!pathInfo.Path.AsSpan().EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var mediaInfo = await GetMediaInfo(pathInfo.Path, _type, cancellationToken).ConfigureAwait(false);

                        if (mediaInfo.MediaStreams.Count == 1)
                        {
                            MediaStream mediaStream = mediaInfo.MediaStreams[0];

                            if ((mediaStream.Type == MediaStreamType.Audio && _type == DlnaProfileType.Audio)
                                || (mediaStream.Type == MediaStreamType.Subtitle && _type == DlnaProfileType.Subtitle))
                            {
                                mediaStream.Index = startIndex++;
                                mediaStream.IsDefault = pathInfo.IsDefault;
                                mediaStream.IsForced = pathInfo.IsForced || mediaStream.IsForced;
                                mediaStream.IsHearingImpaired = pathInfo.IsHearingImpaired || mediaStream.IsHearingImpaired;

                                mediaStreams.Add(MergeMetadata(mediaStream, pathInfo));
                            }
                        }
                        else
                        {
                            foreach (MediaStream mediaStream in mediaInfo.MediaStreams)
                            {
                                if ((mediaStream.Type == MediaStreamType.Audio && _type == DlnaProfileType.Audio)
                                    || (mediaStream.Type == MediaStreamType.Subtitle && _type == DlnaProfileType.Subtitle))
                                {
                                    mediaStream.Index = startIndex++;

                                    mediaStreams.Add(MergeMetadata(mediaStream, pathInfo));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting external streams from {Path}", pathInfo.Path);

                        continue;
                    }
                }
            }

            return mediaStreams;
        }

        /// <summary>
        /// Retrieves the external streams for the provided audio.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/> object to search external streams for.</param>
        /// <param name="startIndex">The stream index to start adding external streams at.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external streams located.</returns>
        public IReadOnlyList<MediaStream> GetExternalStreams(
            Audio audio,
            int startIndex,
            IDirectoryService directoryService,
            bool clearCache)
        {
            if (!audio.IsFileProtocol)
            {
                return Array.Empty<MediaStream>();
            }

            var pathInfos = GetExternalFiles(audio, directoryService, clearCache);

            if (pathInfos.Count == 0)
            {
                return Array.Empty<MediaStream>();
            }

            var mediaStreams = new MediaStream[pathInfos.Count];

            for (var i = 0; i < pathInfos.Count; i++)
            {
                mediaStreams[i] = new MediaStream
                {
                    Type = MediaStreamType.Lyric,
                    Path = pathInfos[i].Path,
                    Language = pathInfos[i].Language,
                    Index = startIndex++
                };
            }

            return mediaStreams;
        }

        /// <summary>
        /// Returns the external file infos for the given video.
        /// </summary>
        /// <param name="video">The <see cref="Video"/> object to search external files for.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external file paths located.</returns>
        public IReadOnlyList<ExternalPathParserResult> GetExternalFiles(
            Video video,
            IDirectoryService directoryService,
            bool clearCache)
        {
            if (!video.IsFileProtocol)
            {
                return Array.Empty<ExternalPathParserResult>();
            }

            // Check if video folder exists
            string folder = video.ContainingFolderPath;
            if (!_fileSystem.DirectoryExists(folder))
            {
                return Array.Empty<ExternalPathParserResult>();
            }

            var files = directoryService.GetFilePaths(folder, clearCache, true).ToList();
            files.Remove(video.Path);
            var internalMetadataPath = video.GetInternalMetadataPath();
            if (_fileSystem.DirectoryExists(internalMetadataPath))
            {
                files.AddRange(directoryService.GetFilePaths(internalMetadataPath, clearCache, true));
            }

            if (files.Count == 0)
            {
                return Array.Empty<ExternalPathParserResult>();
            }

            var externalPathInfos = new List<ExternalPathParserResult>();
            ReadOnlySpan<char> prefix = video.FileNameWithoutExtension;
            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.AsSpan());
                if (fileNameWithoutExtension.Length >= prefix.Length
                    && prefix.Equals(fileNameWithoutExtension[..prefix.Length], StringComparison.OrdinalIgnoreCase)
                    && (fileNameWithoutExtension.Length == prefix.Length || _namingOptions.MediaFlagDelimiters.Contains(fileNameWithoutExtension[prefix.Length])))
                {
                    var externalPathInfo = _externalPathParser.ParseFile(file, fileNameWithoutExtension[prefix.Length..].ToString());

                    if (externalPathInfo is not null)
                    {
                        externalPathInfos.Add(externalPathInfo);
                    }
                }
            }

            return externalPathInfos;
        }

        /// <summary>
        /// Returns the external file infos for the given audio.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/> object to search external files for.</param>
        /// <param name="directoryService">The directory service to search for files.</param>
        /// <param name="clearCache">True if the directory service cache should be cleared before searching.</param>
        /// <returns>The external file paths located.</returns>
        public IReadOnlyList<ExternalPathParserResult> GetExternalFiles(
            Audio audio,
            IDirectoryService directoryService,
            bool clearCache)
        {
            if (!audio.IsFileProtocol)
            {
                return Array.Empty<ExternalPathParserResult>();
            }

            string folder = audio.ContainingFolderPath;
            var files = directoryService.GetFilePaths(folder, clearCache, true).ToList();
            files.Remove(audio.Path);
            var internalMetadataPath = audio.GetInternalMetadataPath();
            if (_fileSystem.DirectoryExists(internalMetadataPath))
            {
                files.AddRange(directoryService.GetFilePaths(internalMetadataPath, clearCache, true));
            }

            if (files.Count == 0)
            {
                return Array.Empty<ExternalPathParserResult>();
            }

            var externalPathInfos = new List<ExternalPathParserResult>();
            ReadOnlySpan<char> prefix = audio.FileNameWithoutExtension;
            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.AsSpan());
                if (fileNameWithoutExtension.Length >= prefix.Length
                    && prefix.Equals(fileNameWithoutExtension[..prefix.Length], StringComparison.OrdinalIgnoreCase)
                    && (fileNameWithoutExtension.Length == prefix.Length || _namingOptions.MediaFlagDelimiters.Contains(fileNameWithoutExtension[prefix.Length])))
                {
                    var externalPathInfo = _externalPathParser.ParseFile(file, fileNameWithoutExtension[prefix.Length..].ToString());

                    if (externalPathInfo is not null)
                    {
                        externalPathInfos.Add(externalPathInfo);
                    }
                }
            }

            return externalPathInfos;
        }

        /// <summary>
        /// Returns the media info of the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The media info for the given file.</returns>
        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(string path, DlnaProfileType type, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = type,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File
                    }
                },
                cancellationToken);
        }

        /// <summary>
        /// Merges path metadata into stream metadata.
        /// </summary>
        /// <param name="mediaStream">The <see cref="MediaStream"/> object.</param>
        /// <param name="pathInfo">The <see cref="ExternalPathParserResult"/> object.</param>
        /// <returns>The modified mediaStream.</returns>
        private MediaStream MergeMetadata(MediaStream mediaStream, ExternalPathParserResult pathInfo)
        {
            mediaStream.Path = pathInfo.Path;
            mediaStream.IsExternal = true;
            mediaStream.Title = string.IsNullOrEmpty(mediaStream.Title) ? (string.IsNullOrEmpty(pathInfo.Title) ? null : pathInfo.Title) : mediaStream.Title;
            mediaStream.Language = string.IsNullOrEmpty(mediaStream.Language) ? (string.IsNullOrEmpty(pathInfo.Language) ? null : pathInfo.Language) : mediaStream.Language;

            return mediaStream;
        }
    }
}
