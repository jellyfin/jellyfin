using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Subtitles
{
    public class SubtitleManager : ISubtitleManager
    {
        private ISubtitleProvider[] _subtitleProviders;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _monitor;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        public event EventHandler<SubtitleDownloadEventArgs> SubtitlesDownloaded;
        public event EventHandler<SubtitleDownloadFailureEventArgs> SubtitleDownloadFailure;

        public SubtitleManager(ILogger logger, IFileSystem fileSystem, ILibraryMonitor monitor, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _monitor = monitor;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
        }

        public void AddParts(IEnumerable<ISubtitleProvider> subtitleProviders)
        {
            _subtitleProviders = subtitleProviders.ToArray();
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            var contentType = request.ContentType;
            var providers = _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(contentType))
                .ToList();

            // If not searching all, search one at a time until something is found
            if (!request.SearchAllProviders)
            {
                foreach (var provider in providers)
                {
                    try
                    {
                        var searchResults = await provider.Search(request, cancellationToken).ConfigureAwait(false);

                        var list = searchResults.ToList();

                        if (list.Count > 0)
                        {
                            Normalize(list);
                            return list;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error downloading subtitles from {0}", ex, provider.Name);
                    }
                }
                return new List<RemoteSubtitleInfo>();
            }

            var tasks = providers.Select(async i =>
            {
                try
                {
                    var searchResults = await i.Search(request, cancellationToken).ConfigureAwait(false);

                    var list = searchResults.ToList();
                    Normalize(list);
                    return list;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading subtitles from {0}", ex, i.Name);
                    return new List<RemoteSubtitleInfo>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        public async Task DownloadSubtitles(Video video,
            string subtitleId,
            CancellationToken cancellationToken)
        {
            var parts = subtitleId.Split(new[] { '_' }, 2);
            var provider = GetProvider(parts.First());

            try
            {
                var response = await GetRemoteSubtitles(subtitleId, cancellationToken).ConfigureAwait(false);

                using (var stream = response.Stream)
                {
                    var savePath = Path.Combine(Path.GetDirectoryName(video.Path),
                        _fileSystem.GetFileNameWithoutExtension(video.Path) + "." + response.Language.ToLower());

                    if (response.IsForced)
                    {
                        savePath += ".forced";
                    }

                    savePath += "." + response.Format.ToLower();

                    _logger.Info("Saving subtitles to {0}", savePath);

                    _monitor.ReportFileSystemChangeBeginning(savePath);

                    try
                    {
                        //var isText = MediaStream.IsTextFormat(response.Format);

                        using (var fs = _fileSystem.GetFileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                        {
                            await stream.CopyToAsync(fs).ConfigureAwait(false);
                        }

                        EventHelper.FireEventIfNotNull(SubtitlesDownloaded, this, new SubtitleDownloadEventArgs
                        {
                            Item = video,
                            Format = response.Format,
                            Language = response.Language,
                            IsForced = response.IsForced,
                            Provider = provider.Name

                        }, _logger);
                    }
                    finally
                    {
                        _monitor.ReportFileSystemChangeComplete(savePath, false);
                    }
                }
            }
            catch (Exception ex)
            {
                EventHelper.FireEventIfNotNull(SubtitleDownloadFailure, this, new SubtitleDownloadFailureEventArgs
                {
                    Item = video,
                    Exception = ex,
                    Provider = provider.Name

                }, _logger);
                
                throw;
            }
        }

        public Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(Video video, string language, CancellationToken cancellationToken)
        {
            if (video.LocationType != LocationType.FileSystem ||
                video.VideoType != VideoType.VideoFile)
            {
                return Task.FromResult<IEnumerable<RemoteSubtitleInfo>>(new List<RemoteSubtitleInfo>());
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
                return Task.FromResult<IEnumerable<RemoteSubtitleInfo>>(new List<RemoteSubtitleInfo>());
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
                RuntimeTicks = video.RunTimeTicks
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

        public Task DeleteSubtitles(string itemId, int index)
        {
            var stream = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                Index = index,
                ItemId = new Guid(itemId),
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

            return _libraryManager.GetItemById(itemId).RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
            {
                ImageRefreshMode = ImageRefreshMode.ValidationOnly,
                MetadataRefreshMode = MetadataRefreshMode.ValidationOnly

            }, CancellationToken.None);
        }

        public Task<SubtitleResponse> GetRemoteSubtitles(string id, CancellationToken cancellationToken)
        {
            var parts = id.Split(new[] { '_' }, 2);

            var provider = GetProvider(parts.First());
            id = parts.Last();

            return provider.GetSubtitles(id, cancellationToken);
        }

        public IEnumerable<SubtitleProviderInfo> GetProviders(string itemId)
        {
            var video = _libraryManager.GetItemById(itemId) as Video;
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
                return new List<SubtitleProviderInfo>();
            }

            var providers = _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(mediaType))
                .ToList();

            return providers.Select(i => new SubtitleProviderInfo
            {
                Name = i.Name,
                Id = GetProviderId(i.Name)
            });
        }

    }
}
