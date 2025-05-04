#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Subtitles
{
    public class SubtitleManager : ISubtitleManager
    {
        private readonly ILogger<SubtitleManager> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _monitor;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILocalizationManager _localization;

        private readonly ISubtitleProvider[] _subtitleProviders;

        public SubtitleManager(
            ILogger<SubtitleManager> logger,
            IFileSystem fileSystem,
            ILibraryMonitor monitor,
            IMediaSourceManager mediaSourceManager,
            ILocalizationManager localizationManager,
            IEnumerable<ISubtitleProvider> subtitleProviders)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _monitor = monitor;
            _mediaSourceManager = mediaSourceManager;
            _localization = localizationManager;
            _subtitleProviders = subtitleProviders
                .OrderBy(i => i is IHasOrder hasOrder ? hasOrder.Order : 0)
                .ToArray();
        }

        /// <inheritdoc />
        public event EventHandler<SubtitleDownloadFailureEventArgs>? SubtitleDownloadFailure;

        /// <inheritdoc />
        public async Task<RemoteSubtitleInfo[]> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            if (request.Language is not null)
            {
                var culture = _localization.FindLanguageInfo(request.Language);

                if (culture is not null)
                {
                    request.TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName;
                }
            }

            var contentType = request.ContentType;
            var providers = _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(contentType) && !request.DisabledSubtitleFetchers.Contains(i.Name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i =>
                {
                    var index = request.SubtitleFetcherOrder.IndexOf(i.Name);
                    return index == -1 ? int.MaxValue : index;
                })
                .ToArray();

            // If not searching all, search one at a time until something is found
            if (!request.SearchAllProviders)
            {
                foreach (var provider in providers)
                {
                    try
                    {
                        var searchResults = await provider.Search(request, cancellationToken).ConfigureAwait(false);

                        var list = searchResults.ToArray();

                        if (list.Length > 0)
                        {
                            Normalize(list);
                            return list;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading subtitles from {Provider}", provider.Name);
                    }
                }

                return Array.Empty<RemoteSubtitleInfo>();
            }

            var tasks = providers.Select(async i =>
            {
                try
                {
                    var searchResults = await i.Search(request, cancellationToken).ConfigureAwait(false);

                    var list = searchResults.ToArray();
                    Normalize(list);
                    return list;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading subtitles from {Name}", i.Name);
                    return Array.Empty<RemoteSubtitleInfo>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i).ToArray();
        }

        /// <inheritdoc />
        public Task DownloadSubtitles(Video video, string subtitleId, CancellationToken cancellationToken)
        {
            var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(video);

            return DownloadSubtitles(video, libraryOptions, subtitleId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DownloadSubtitles(
            Video video,
            LibraryOptions libraryOptions,
            string subtitleId,
            CancellationToken cancellationToken)
        {
            var parts = subtitleId.Split('_', 2);
            var provider = GetProvider(parts[0]);

            try
            {
                var response = await GetRemoteSubtitles(subtitleId, cancellationToken).ConfigureAwait(false);

                await TrySaveSubtitle(video, libraryOptions, response).ConfigureAwait(false);
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SubtitleDownloadFailure?.Invoke(this, new SubtitleDownloadFailureEventArgs
                {
                    Item = video,
                    Exception = ex,
                    Provider = provider.Name
                });

                throw;
            }
        }

        /// <inheritdoc />
        public Task UploadSubtitle(Video video, SubtitleResponse response)
        {
            var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(video);
            return TrySaveSubtitle(video, libraryOptions, response);
        }

        private async Task TrySaveSubtitle(
            Video video,
            LibraryOptions libraryOptions,
            SubtitleResponse response)
        {
            var saveInMediaFolder = libraryOptions.SaveSubtitlesWithMedia;

            var memoryStream = new MemoryStream();
            await using (memoryStream.ConfigureAwait(false))
            {
                var stream = response.Stream;
                await using (stream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                    memoryStream.Position = 0;
                }

                var savePaths = new List<string>();
                var saveFileName = Path.GetFileNameWithoutExtension(video.Path) + "." + response.Language.ToLowerInvariant();

                if (response.IsForced)
                {
                    saveFileName += ".forced";
                }

                if (response.IsHearingImpaired)
                {
                    saveFileName += ".sdh";
                }

                if (saveInMediaFolder)
                {
                    var mediaFolderPath = Path.GetFullPath(Path.Combine(video.ContainingFolderPath, saveFileName));
                    savePaths.Add(mediaFolderPath);
                }

                var internalPath = Path.GetFullPath(Path.Combine(video.GetInternalMetadataPath(), saveFileName));

                savePaths.Add(internalPath);

                await TrySaveToFiles(memoryStream, savePaths, video, response.Format.ToLowerInvariant()).ConfigureAwait(false);
            }
        }

        private async Task TrySaveToFiles(Stream stream, List<string> savePaths, Video video, string extension)
        {
            List<Exception>? exs = null;

            foreach (var savePath in savePaths)
            {
                var path = savePath + "." + extension;
                try
                {
                    if (path.StartsWith(video.ContainingFolderPath, StringComparison.Ordinal)
                            || path.StartsWith(video.GetInternalMetadataPath(), StringComparison.Ordinal))
                    {
                        var fileExists = File.Exists(path);
                        var counter = 0;

                        while (fileExists)
                        {
                            path = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", savePath, counter, extension);
                            fileExists = File.Exists(path);
                            counter++;
                        }

                        _logger.LogInformation("Saving subtitles to {SavePath}", path);
                        _monitor.ReportFileSystemChangeBeginning(path);

                        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Path can't be a root directory."));

                        var fileOptions = AsyncFile.WriteOptions;
                        fileOptions.Mode = FileMode.CreateNew;
                        fileOptions.PreallocationSize = stream.Length;
                        var fs = new FileStream(path, fileOptions);
                        await using (fs.ConfigureAwait(false))
                        {
                            await stream.CopyToAsync(fs).ConfigureAwait(false);
                        }

                        return;
                    }
                    else
                    {
                        // TODO: Add some error handling to the API user: return BadRequest("Could not save subtitle, bad path.");
                        _logger.LogError("An uploaded subtitle could not be saved because the resulting path was invalid.");
                    }
                }
                catch (Exception ex)
                {
                    (exs ??= []).Add(ex);
                }
                finally
                {
                    _monitor.ReportFileSystemChangeComplete(path, false);
                }

                stream.Position = 0;
            }

            if (exs is not null)
            {
                throw new AggregateException(exs);
            }
        }

        /// <inheritdoc />
        public Task<RemoteSubtitleInfo[]> SearchSubtitles(Video video, string language, bool? isPerfectMatch, bool isAutomated, CancellationToken cancellationToken)
        {
            if (video.VideoType != VideoType.VideoFile)
            {
                return Task.FromResult(Array.Empty<RemoteSubtitleInfo>());
            }

            VideoContentType mediaType;

            if (video is Episode)
            {
                mediaType = VideoContentType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = VideoContentType.Movie;
            }
            else
            {
                // These are the only supported types
                return Task.FromResult(Array.Empty<RemoteSubtitleInfo>());
            }

            var request = new SubtitleSearchRequest
            {
                ContentType = mediaType,
                IndexNumber = video.IndexNumber,
                Language = language,
                MediaPath = video.Path,
                Name = video.Name,
                ParentIndexNumber = video.ParentIndexNumber,
                ProductionYear = video.ProductionYear,
                ProviderIds = video.ProviderIds,
                RuntimeTicks = video.RunTimeTicks,
                IsPerfectMatch = isPerfectMatch ?? false,
                IsAutomated = isAutomated
            };

            if (video is Episode episode)
            {
                request.IndexNumberEnd = episode.IndexNumberEnd;
                request.SeriesName = episode.SeriesName;
            }

            return SearchSubtitles(request, cancellationToken);
        }

        private void Normalize(IEnumerable<RemoteSubtitleInfo> subtitles)
        {
            foreach (var sub in subtitles)
            {
                sub.Id = GetProviderId(sub.ProviderName) + "_" + sub.Id;
            }
        }

        private string GetProviderId(string name)
        {
            return name.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        private ISubtitleProvider GetProvider(string id)
        {
            return _subtitleProviders.First(i => string.Equals(id, GetProviderId(i.Name), StringComparison.Ordinal));
        }

        /// <inheritdoc />
        public Task DeleteSubtitles(BaseItem item, int index)
        {
            var stream = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                Index = index,
                ItemId = item.Id,
                Type = MediaStreamType.Subtitle
            })[0];

            var path = stream.Path;
            _monitor.ReportFileSystemChangeBeginning(path);

            try
            {
                _fileSystem.DeleteFile(path);
            }
            finally
            {
                _monitor.ReportFileSystemChangeComplete(path, false);
            }

            return item.RefreshMetadata(CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<SubtitleResponse> GetRemoteSubtitles(string id, CancellationToken cancellationToken)
        {
            var parts = id.Split('_', 2);

            var provider = GetProvider(parts[0]);
            id = parts[^1];

            return provider.GetSubtitles(id, cancellationToken);
        }

        /// <inheritdoc />
        public SubtitleProviderInfo[] GetSupportedProviders(BaseItem item)
        {
            VideoContentType mediaType;

            if (item is Episode)
            {
                mediaType = VideoContentType.Episode;
            }
            else if (item is Movie)
            {
                mediaType = VideoContentType.Movie;
            }
            else
            {
                // These are the only supported types
                return Array.Empty<SubtitleProviderInfo>();
            }

            return _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(mediaType))
                .Select(i => new SubtitleProviderInfo
                {
                    Name = i.Name,
                    Id = GetProviderId(i.Name)
                }).ToArray();
        }
    }
}
