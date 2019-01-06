using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Providers.Subtitles
{
    public class SubtitleManager : ISubtitleManager
    {
        private ISubtitleProvider[] _subtitleProviders;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _monitor;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerConfigurationManager _config;

        public event EventHandler<SubtitleDownloadEventArgs> SubtitlesDownloaded;
        public event EventHandler<SubtitleDownloadFailureEventArgs> SubtitleDownloadFailure;

        private ILocalizationManager _localization;

        public SubtitleManager(ILogger logger, IFileSystem fileSystem, ILibraryMonitor monitor, IMediaSourceManager mediaSourceManager, IServerConfigurationManager config, ILocalizationManager localizationManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _monitor = monitor;
            _mediaSourceManager = mediaSourceManager;
            _config = config;
            _localization = localizationManager;
        }

        public void AddParts(IEnumerable<ISubtitleProvider> subtitleProviders)
        {
            _subtitleProviders = subtitleProviders
                .OrderBy(i =>
                {
                    var hasOrder = i as IHasOrder;
                    return hasOrder == null ? 0 : hasOrder.Order;
                })
                .ToArray();
        }

        public async Task<RemoteSubtitleInfo[]> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            if (request.Language != null)
            {
                var culture = _localization.FindLanguageInfo(request.Language);

                if (culture != null)
                {
                    request.TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName;
                }
            }

            var contentType = request.ContentType;
            var providers = _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(contentType))
                .Where(i => !request.DisabledSubtitleFetchers.Contains(i.Name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(i =>
                {
                    var index = request.SubtitleFetcherOrder.ToList().IndexOf(i.Name);
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
                return new RemoteSubtitleInfo[] { };
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
                    _logger.LogError(ex, "Error downloading subtitles from {0}", i.Name);
                    return new RemoteSubtitleInfo[] { };
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i).ToArray();
        }

        private SubtitleOptions GetOptions()
        {
            return _config.GetConfiguration<SubtitleOptions>("subtitles");
        }

        public Task DownloadSubtitles(Video video, string subtitleId, CancellationToken cancellationToken)
        {
            var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(video);

            return DownloadSubtitles(video, libraryOptions, subtitleId, cancellationToken);
        }

        public async Task DownloadSubtitles(Video video,
            LibraryOptions libraryOptions,
            string subtitleId,
            CancellationToken cancellationToken)
        {
            var parts = subtitleId.Split(new[] { '_' }, 2);
            var provider = GetProvider(parts.First());

            var saveInMediaFolder = libraryOptions.SaveSubtitlesWithMedia;

            try
            {
                var response = await GetRemoteSubtitles(subtitleId, cancellationToken).ConfigureAwait(false);

                using (var stream = response.Stream)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                        memoryStream.Position = 0;

                        var savePaths = new List<string>();
                        var saveFileName = _fileSystem.GetFileNameWithoutExtension(video.Path) + "." + response.Language.ToLower();

                        if (response.IsForced)
                        {
                            saveFileName += ".forced";
                        }

                        saveFileName += "." + response.Format.ToLower();

                        if (saveInMediaFolder)
                        {
                            savePaths.Add(Path.Combine(video.ContainingFolderPath, saveFileName));
                        }

                        savePaths.Add(Path.Combine(video.GetInternalMetadataPath(), saveFileName));

                        await TrySaveToFiles(memoryStream, savePaths).ConfigureAwait(false);
                    }
                }
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

        private async Task TrySaveToFiles(Stream stream, List<string> savePaths)
        {
            Exception exceptionToThrow = null;

            foreach (var savePath in savePaths)
            {
                _logger.LogInformation("Saving subtitles to {0}", savePath);

                _monitor.ReportFileSystemChangeBeginning(savePath);

                try
                {
                    _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(savePath));

                    using (var fs = _fileSystem.GetFileStream(savePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true))
                    {
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    if (exceptionToThrow == null)
                    {
                        exceptionToThrow = ex;
                    }
                }
                finally
                {
                    _monitor.ReportFileSystemChangeComplete(savePath, false);
                }

                stream.Position = 0;
            }

            if (exceptionToThrow != null)
            {
                throw exceptionToThrow;
            }
        }

        public Task<RemoteSubtitleInfo[]> SearchSubtitles(Video video, string language, bool? isPerfectMatch, CancellationToken cancellationToken)
        {
            if (video.VideoType != VideoType.VideoFile)
            {
                return Task.FromResult<RemoteSubtitleInfo[]>(new RemoteSubtitleInfo[] { });
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
                return Task.FromResult<RemoteSubtitleInfo[]>(new RemoteSubtitleInfo[] { });
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
                IsPerfectMatch = isPerfectMatch ?? false
            };

            var episode = video as Episode;

            if (episode != null)
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
            return name.ToLower().GetMD5().ToString("N");
        }

        private ISubtitleProvider GetProvider(string id)
        {
            return _subtitleProviders.First(i => string.Equals(id, GetProviderId(i.Name)));
        }

        public Task DeleteSubtitles(BaseItem item, int index)
        {
            var stream = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                Index = index,
                ItemId = item.Id,
                Type = MediaStreamType.Subtitle

            }).First();

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

        public Task<SubtitleResponse> GetRemoteSubtitles(string id, CancellationToken cancellationToken)
        {
            var parts = id.Split(new[] { '_' }, 2);

            var provider = GetProvider(parts.First());
            id = parts.Last();

            return provider.GetSubtitles(id, cancellationToken);
        }

        public SubtitleProviderInfo[] GetSupportedProviders(BaseItem video)
        {
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
                return new SubtitleProviderInfo[] { };
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
