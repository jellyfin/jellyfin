#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Jellyfin.XmlTv;
using Jellyfin.XmlTv.Entities;
using Jellyfin.XmlTv.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings
{
    public class XmlTvListingsProvider : IListingsProvider
    {
        private static readonly TimeSpan _maxCacheAge = TimeSpan.FromHours(1);
        private static readonly TimeSpan _downloadTimeout = TimeSpan.FromMinutes(15);

        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<XmlTvListingsProvider> _logger;

        private readonly ConcurrentDictionary<string, DateTime> _lastDownloadFailures = new(StringComparer.Ordinal);

        public XmlTvListingsProvider(
            IServerConfigurationManager config,
            IHttpClientFactory httpClientFactory,
            ILogger<XmlTvListingsProvider> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "XmlTV";

        public string Type => "xmltv";

        private string GetLanguage(ListingsProviderInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.PreferredLanguage))
            {
                return info.PreferredLanguage;
            }

            return _config.Configuration.PreferredMetadataLanguage;
        }

        private async Task<string> GetXml(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation("xmltv path: {Path}", info.Path);

            string cacheFilename = info.Id + ".xml";
            string cacheDir = Path.Join(_config.ApplicationPaths.CachePath, "xmltv");
            string cacheFile = Path.Join(cacheDir, cacheFilename);

            if (File.Exists(cacheFile) && File.GetLastWriteTimeUtc(cacheFile) >= DateTime.UtcNow.Subtract(_maxCacheAge))
            {
                return cacheFile;
            }

            var isRemote = info.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase);

            if (isRemote
                && _lastDownloadFailures.TryGetValue(info.Path, out var lastFailure)
                && DateTime.UtcNow - lastFailure < _maxCacheAge)
            {
                if (File.Exists(cacheFile))
                {
                    return cacheFile;
                }

                throw new InvalidOperationException("Skipping the XMLTV download after a recent failure: " + info.Path);
            }

            Directory.CreateDirectory(cacheDir);

            var tempFile = cacheFile + ".tmp";

            try
            {
                using var timeout = new CancellationTokenSource(_downloadTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                var downloadCancellationToken = linkedTokenSource.Token;

                if (isRemote)
                {
                    _logger.LogInformation("Downloading xmltv listings from {Path}", info.Path);

                    var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
                    httpClient.Timeout = _downloadTimeout;

                    using var response = await httpClient
                        .GetAsync(info.Path, HttpCompletionOption.ResponseHeadersRead, downloadCancellationToken)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var redirectedUrl = response.RequestMessage?.RequestUri?.ToString() ?? info.Path;
                    var stream = await response.Content.ReadAsStreamAsync(downloadCancellationToken).ConfigureAwait(false);
                    await using (stream.ConfigureAwait(false))
                    {
                        await UnzipIfNeededAndCopy(redirectedUrl, stream, tempFile, downloadCancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var stream = AsyncFile.OpenRead(info.Path);
                    await using (stream.ConfigureAwait(false))
                    {
                        await UnzipIfNeededAndCopy(info.Path, stream, tempFile, downloadCancellationToken).ConfigureAwait(false);
                    }
                }

                File.Move(tempFile, cacheFile, true);
                _lastDownloadFailures.TryRemove(info.Path, out _);

                return cacheFile;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TryDeleteTempFile(tempFile);

                throw;
            }
            catch (Exception ex)
            {
                TryDeleteTempFile(tempFile);
                _lastDownloadFailures[info.Path] = DateTime.UtcNow;

                _logger.LogError(ex, "Error downloading or processing XMLTV file from {Path}", info.Path);

                if (File.Exists(cacheFile))
                {
                    _logger.LogWarning("Falling back to the previously downloaded XMLTV file for {Path}", info.Path);

                    return cacheFile;
                }

                if (ex is OperationCanceledException)
                {
                    throw new TimeoutException(
                        string.Format(CultureInfo.InvariantCulture, "Timed out downloading the XMLTV file from {0}", info.Path),
                        ex);
                }

                throw;
            }
        }

        private void TryDeleteTempFile(string tempFile)
        {
            try
            {
                File.Delete(tempFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Error deleting temporary XMLTV file {File}", tempFile);
            }
        }

        private async Task UnzipIfNeededAndCopy(string originalUrl, Stream stream, string file, CancellationToken cancellationToken)
        {
            var fileStream = new FileStream(
                file,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                IODefaults.FileStreamBufferSize,
                FileOptions.Asynchronous);

            await using (fileStream.ConfigureAwait(false))
            {
                if (Path.GetExtension(originalUrl.AsSpan().LeftPart('?')).Equals(".gz", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(originalUrl.AsSpan().LeftPart('?')).Equals(".gzip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var reader = new GZipStream(stream, CompressionMode.Decompress);
                        await reader.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extracting from gz file {File}", originalUrl);
                    }
                }
                else
                {
                    await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }

            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                throw new InvalidOperationException("Downloaded XMLTV file is empty: " + originalUrl);
            }
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            _logger.LogDebug("Getting xmltv programs for channel {Id}", channelId);

            string path = await GetXml(info, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {Path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));

            return reader.GetProgrammes(channelId, startDateUtc, endDateUtc, cancellationToken)
                        .Select(p => GetProgramInfoWithEtag(p, info));
        }

        private ProgramInfo GetProgramInfoWithEtag(XmlTvProgram program, ListingsProviderInfo info)
        {
            var programInfo = GetProgramInfo(program, info);

            if (XmlTvProgramEtag.TryCreate(programInfo, out var etag, out var reason))
            {
                programInfo.Etag = etag;
            }
            else
            {
                _logger.LogDebug(
                    "Unable to create XMLTV program ETag for program {ProgramId} on channel {ChannelId} from {StartDate} to {EndDate}: {Reason}. The program will be treated as updated on each guide refresh.",
                    programInfo.Id,
                    programInfo.ChannelId,
                    programInfo.StartDate,
                    programInfo.EndDate,
                    reason);
            }

            return programInfo;
        }

        private static ProgramInfo GetProgramInfo(XmlTvProgram program, ListingsProviderInfo info)
        {
            string? episodeTitle = program.Episode?.Title;
            var programCategories = program.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            var imageUrl = program.Icons.FirstOrDefault()?.Source;
            var episodeImageUrl = program.Images?.FirstOrDefault(m => m.Type == ImageType.Still)?.Path;
            var backgroundImageUrl = program.Images?.FirstOrDefault(m => m.Type == ImageType.Backdrop)?.Path;
            var rating = program.Ratings.FirstOrDefault()?.Value;
            var starRating = program.StarRatings?.FirstOrDefault()?.StarRating;

            var programInfo = new ProgramInfo
            {
                ChannelId = program.ChannelId,
                EndDate = program.EndDate.UtcDateTime,
                EpisodeNumber = program.Episode?.Episode,
                EpisodeTitle = episodeTitle,
                Genres = programCategories,
                StartDate = program.StartDate.UtcDateTime,
                Name = program.Title,
                Overview = program.Description,
                ProductionYear = program.CopyrightDate?.Year,
                SeasonNumber = program.Episode?.Series,
                IsSeries = program.Episode?.Episode is not null,
                IsRepeat = program.IsPreviouslyShown && !program.IsNew,
                IsPremiere = program.Premiere is not null,
                IsLive = program.IsLive,
                IsKids = programCategories.Any(c => info.KidsCategories.Contains(c, StringComparison.OrdinalIgnoreCase)),
                IsMovie = programCategories.Any(c => info.MovieCategories.Contains(c, StringComparison.OrdinalIgnoreCase)),
                IsNews = programCategories.Any(c => info.NewsCategories.Contains(c, StringComparison.OrdinalIgnoreCase)),
                IsSports = programCategories.Any(c => info.SportsCategories.Contains(c, StringComparison.OrdinalIgnoreCase)),
                ImageUrl = string.IsNullOrEmpty(imageUrl) ? null : imageUrl,
                HasImage = !string.IsNullOrEmpty(imageUrl),
                BackdropImageUrl = string.IsNullOrEmpty(backgroundImageUrl) ? null : backgroundImageUrl,
                ThumbImageUrl = string.IsNullOrEmpty(episodeImageUrl) ? null : episodeImageUrl,
                OfficialRating = string.IsNullOrEmpty(rating) ? null : rating,
                CommunityRating = starRating is null ? null : (float)starRating.Value,
                SeriesId = program.Episode?.Episode is null ? null : program.Title?.GetMD5().ToString("N", CultureInfo.InvariantCulture)
            };

            if (string.IsNullOrWhiteSpace(program.ProgramId))
            {
                string uniqueString = (program.Title ?? string.Empty) + (episodeTitle ?? string.Empty);

                if (programInfo.SeasonNumber.HasValue)
                {
                    uniqueString = "-" + programInfo.SeasonNumber.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (programInfo.EpisodeNumber.HasValue)
                {
                    uniqueString = "-" + programInfo.EpisodeNumber.Value.ToString(CultureInfo.InvariantCulture);
                }

                programInfo.ShowId = uniqueString.GetMD5().ToString("N", CultureInfo.InvariantCulture);

                // If we don't have valid episode info, assume it's a unique program, otherwise recordings might be skipped
                if (programInfo.IsSeries
                    && !programInfo.IsRepeat
                    && (programInfo.EpisodeNumber ?? 0) == 0)
                {
                    programInfo.ShowId += programInfo.StartDate.Ticks.ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                programInfo.ShowId = program.ProgramId;
            }

            // Construct an id from the channel and start date
            programInfo.Id = string.Format(CultureInfo.InvariantCulture, "{0}_{1:O}", program.ChannelId, program.StartDate);

            if (programInfo.IsMovie)
            {
                programInfo.IsSeries = false;
                programInfo.EpisodeNumber = null;
                programInfo.EpisodeTitle = null;
            }

            return programInfo;
        }

        public Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            // Saving the provider is an explicit retry, so the download backoff has to be dropped
            // together with the cached file the listings manager deletes.
            if (!string.IsNullOrEmpty(info.Path))
            {
                _lastDownloadFailures.TryRemove(info.Path, out _);
            }

            // Assume all urls are valid. check files for existence
            if (!info.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !File.Exists(info.Path))
            {
                throw new FileNotFoundException("Could not find the XmlTv file specified:", info.Path);
            }

            return Task.CompletedTask;
        }

        public async Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            // In theory this should never be called because there is always only one lineup
            string path = await GetXml(info, CancellationToken.None).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {Path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));
            IEnumerable<XmlTvChannel> results = reader.GetChannels();

            // Should this method be async?
            return results.Select(c => new NameIdPair() { Id = c.Id, Name = c.DisplayName }).ToList();
        }

        public async Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            // In theory this should never be called because there is always only one lineup
            string path = await GetXml(info, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {Path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));
            var results = reader.GetChannels();

            // Should this method be async?
            return results.Select(c => new ChannelInfo
            {
                Id = c.Id,
                Name = c.DisplayName,
                ImageUrl = string.IsNullOrEmpty(c.Icons.FirstOrDefault()?.Source) ? null : c.Icons.FirstOrDefault()!.Source,
                Number = string.IsNullOrWhiteSpace(c.Number) ? c.Id : c.Number
            }).ToList();
        }
    }
}
