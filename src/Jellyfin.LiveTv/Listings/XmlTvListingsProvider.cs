#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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

        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<XmlTvListingsProvider> _logger;

        // Caches the last fully-parsed XMLTV file, grouped by channel id, so a guide refresh over
        // thousands of channels parses the (potentially multi-GB) file once instead of once per
        // channel. Keyed by file path + last-write-time so a freshly downloaded file rebuilds it.
        private readonly object _programsCacheLock = new();
        private string? _programsCacheFile;
        private DateTime _programsCacheWriteTimeUtc;
        private IReadOnlyDictionary<string, List<XmlTvProgram>>? _programsCache;

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
            _logger.LogDebug("xmltv path: {Path}", info.Path);

            string cacheFilename = info.Id + ".xml";
            string cacheDir = Path.Join(_config.ApplicationPaths.CachePath, "xmltv");
            string cacheFile = Path.Join(cacheDir, cacheFilename);

            if (File.Exists(cacheFile))
            {
                if (File.GetLastWriteTimeUtc(cacheFile) >= DateTime.UtcNow.Subtract(_maxCacheAge))
                {
                    return cacheFile;
                }

                File.Delete(cacheFile);
            }
            else
            {
                Directory.CreateDirectory(cacheDir);
            }

            try
            {
                if (info.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Downloading xmltv listings from {Path}", info.Path);

                    using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(info.Path, cancellationToken).ConfigureAwait(false);
                    var redirectedUrl = response.RequestMessage?.RequestUri?.ToString() ?? info.Path;
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    await using (stream.ConfigureAwait(false))
                    {
                        return await UnzipIfNeededAndCopy(redirectedUrl, stream, cacheFile, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var stream = AsyncFile.OpenRead(info.Path);
                    await using (stream.ConfigureAwait(false))
                    {
                        return await UnzipIfNeededAndCopy(info.Path, stream, cacheFile, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading or processing XMLTV file from {Path}", info.Path);

                if (File.Exists(cacheFile))
                {
                    File.Delete(cacheFile);
                }

                throw;
            }
        }

        private async Task<string> UnzipIfNeededAndCopy(string originalUrl, Stream stream, string file, CancellationToken cancellationToken)
        {
            var fileStream = new FileStream(
                file,
                FileMode.CreateNew,
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
                if (fileInfo.Exists)
                {
                    File.Delete(file);
                }

                throw new InvalidOperationException("Downloaded XMLTV file is empty: " + originalUrl);
            }

            return file;
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            _logger.LogDebug("Getting xmltv programs for channel {Id}", channelId);

            string path = await GetXml(info, cancellationToken).ConfigureAwait(false);

            var programsByChannel = GetProgramsByChannel(path, GetLanguage(info), cancellationToken);
            if (!programsByChannel.TryGetValue(channelId, out var channelPrograms))
            {
                return [];
            }

            var startOffset = new DateTimeOffset(DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc));
            var endOffset = new DateTimeOffset(DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc));

            // Materialize fresh ProgramInfo instances per call: callers mutate ProgramInfo (Id/ChannelId),
            // so cached parsed XmlTvProgram entries must never be handed out directly.
            return channelPrograms
                .Where(p => p.EndDate >= startOffset && p.StartDate < endOffset)
                .Select(p => GetProgramInfoWithEtag(p, info))
                .ToList();
        }

        /// <summary>
        /// Parses the entire XMLTV file a single time and groups every programme by channel id, caching
        /// the result until the underlying file changes. Without this, a guide refresh re-parses the whole
        /// file once for every channel, which is unusable for providers exposing tens of thousands of channels.
        /// </summary>
        private IReadOnlyDictionary<string, List<XmlTvProgram>> GetProgramsByChannel(string path, string? language, CancellationToken cancellationToken)
        {
            var writeTimeUtc = File.GetLastWriteTimeUtc(path);

            var cached = _programsCache;
            if (cached is not null
                && string.Equals(_programsCacheFile, path, StringComparison.Ordinal)
                && _programsCacheWriteTimeUtc == writeTimeUtc)
            {
                return cached;
            }

            lock (_programsCacheLock)
            {
                cached = _programsCache;
                if (cached is not null
                    && string.Equals(_programsCacheFile, path, StringComparison.Ordinal)
                    && _programsCacheWriteTimeUtc == writeTimeUtc)
                {
                    return cached;
                }

                _logger.LogDebug("Parsing XMLTV programmes for all channels from {Path}", path);

                // Release any previously cached data before building the replacement.
                _programsCache = null;
                _programsCacheFile = null;

                var parsed = ParseProgramsByChannel(path, language, cancellationToken);

                _programsCacheFile = path;
                _programsCacheWriteTimeUtc = writeTimeUtc;
                _programsCache = parsed;

                _logger.LogDebug("Parsed XMLTV programmes for {ChannelCount} channels from {Path}", parsed.Count, path);

                return parsed;
            }
        }

        private static IReadOnlyDictionary<string, List<XmlTvProgram>> ParseProgramsByChannel(string path, string? language, CancellationToken cancellationToken)
        {
            // Reuse the library's per-programme parser, but drive a single pass over the file ourselves
            // so every channel's programmes are captured in one read instead of one read per channel.
            var reader = new XmlTvReader(path, language);
            var result = new Dictionary<string, List<XmlTvProgram>>(StringComparer.OrdinalIgnoreCase);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            using var xml = XmlReader.Create(path, settings);
            if (xml.ReadToDescendant("tv") && xml.ReadToDescendant("programme"))
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var channelId = xml.GetAttribute("channel");
                    if (string.IsNullOrEmpty(channelId))
                    {
                        continue;
                    }

                    // Capture the full date range; per-request date filtering happens against the cache.
                    var programme = reader.GetProgramme(xml, channelId, DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
                    if (programme is null)
                    {
                        continue;
                    }

                    if (!result.TryGetValue(channelId, out var list))
                    {
                        list = [];
                        result[channelId] = list;
                    }

                    list.Add(programme);
                }
                while (xml.ReadToFollowing("programme"));
            }

            return result;
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
