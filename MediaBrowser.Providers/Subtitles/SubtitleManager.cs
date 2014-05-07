using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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

namespace MediaBrowser.Providers.Subtitles
{
    public class SubtitleManager : ISubtitleManager
    {
        private ISubtitleProvider[] _subtitleProviders;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _monitor;

        public SubtitleManager(ILogger logger, IFileSystem fileSystem, ILibraryMonitor monitor)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _monitor = monitor;
        }

        public void AddParts(IEnumerable<ISubtitleProvider> subtitleProviders)
        {
            _subtitleProviders = subtitleProviders.ToArray();
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            var providers = _subtitleProviders
                .Where(i => i.SupportedMediaTypes.Contains(request.ContentType))
                .ToList();

            var tasks = providers.Select(async i =>
            {
                try
                {
                    return await i.SearchSubtitles(request, cancellationToken).ConfigureAwait(false);
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
            string providerName,
            CancellationToken cancellationToken)
        {
            var provider = _subtitleProviders.First(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));

            var response = await provider.GetSubtitles(subtitleId, cancellationToken).ConfigureAwait(false);

            using (var stream = response.Stream)
            {
                var savePath = Path.Combine(Path.GetDirectoryName(video.Path), 
                    Path.GetFileNameWithoutExtension(video.Path) + "." + response.Language.ToLower() + "." + response.Format.ToLower());

                _logger.Info("Saving subtitles to {0}", savePath);

                _monitor.ReportFileSystemChangeBeginning(savePath);

                try
                {
                    using (var fs = _fileSystem.GetFileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _monitor.ReportFileSystemChangeComplete(savePath, false);
                }
            }
        }

        public Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(Video video, string language, CancellationToken cancellationToken)
        {
            if (video.LocationType != LocationType.FileSystem ||
                video.VideoType != VideoType.VideoFile)
            {
                return Task.FromResult<IEnumerable<RemoteSubtitleInfo>>(new List<RemoteSubtitleInfo>());
            }

            SubtitleMediaType mediaType;

            if (video is Episode)
            {
                mediaType = SubtitleMediaType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = SubtitleMediaType.Movie;
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
                ProviderIds = video.ProviderIds
            };

            var episode = video as Episode;

            if (episode != null)
            {
                request.IndexNumberEnd = episode.IndexNumberEnd;
                request.SeriesName = episode.SeriesName;
            }

            return SearchSubtitles(request, cancellationToken);
        }
    }
}
