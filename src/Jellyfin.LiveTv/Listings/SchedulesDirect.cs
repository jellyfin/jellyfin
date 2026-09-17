#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using Jellyfin.LiveTv.Guide;
using Jellyfin.LiveTv.Listings.SchedulesDirectDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings
{
    public class SchedulesDirect : IListingsProvider, ISchedulesDirectService, IDisposable
    {
        private const string ApiUrl = "https://json.schedulesdirect.org/20141201";
        private const int CountryCacheDays = 7;

        // Schedules Direct disables an account that keeps requesting images after the daily
        // download limit, so stop after a short streak of rejections instead of retrying forever.
        private const int MaxConsecutiveImageFailures = 10;

        // Hard server-side cap on elements in a single programs request.
        private const int MaxProgramsPerRequest = 5000;

        // Memory ceiling on the rejected image uris (~2MB of 64-char keys). The daily invalid-uri
        // limit stops the account long before this many can accumulate.
        private const int MaxRejectedImageUris = 10000;

        // Stations that stopped being queried keep their cached schedules for this long.
        private const int ScheduleCacheMaxAgeDays = 30;

        // The spec requires a client to stay away for an hour once the service reports Offline.
        private const int OfflineBackoffMinutes = 60;

        private const int StatusCacheMinutes = 10;

        private readonly ILogger<SchedulesDirect> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationPaths _appPaths;
        private readonly AsyncNonKeyedLocker _tokenLock = new(1);
        private readonly AsyncNonKeyedLocker _statusLock = new(1);
        private readonly SchedulesDirectScheduleCache _scheduleCache;
        private readonly SchedulesDirectLineupCache _lineupCache;

        // Image uris Schedules Direct answered with IMAGE_NOT_FOUND. Requesting one of these
        // again is what trips MAX_IMAGE_INVALID_URI_ERRORS and gets the account blocked.
        private readonly ConcurrentDictionary<string, byte> _rejectedImageUris = new(StringComparer.Ordinal);

        // Station/date pairs the server has queued for generation, with the retryTime it gave.
        private readonly ConcurrentDictionary<string, DateTime> _scheduleRetryTimes = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, NameValuePair> _tokens = new();
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private long _lastErrorResponseTicks;
        private volatile bool _accountError;
        private bool _disposed;

        private byte[] _countriesCache;
        private DateOnly? _imageLimitHitDate;
        private DateOnly? _metadataLimitHitDate;
        private int _consecutiveImageFailures;
        private long _offlineUntilTicks;
        private StatusCacheEntry _status;

        public SchedulesDirect(
            ILogger<SchedulesDirect> logger,
            IHttpClientFactory httpClientFactory,
            IApplicationPaths appPaths)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appPaths = appPaths;
            _scheduleCache = new SchedulesDirectScheduleCache(logger, appPaths);
            _lineupCache = new SchedulesDirectLineupCache(logger, appPaths);
            _imageLimitHitDate = LoadDailyLimitDate(ImageLimitFilePath);
            _metadataLimitHitDate = LoadDailyLimitDate(MetadataLimitFilePath);
        }

        /// <inheritdoc />
        public string Name => "Schedules Direct";

        private string ImageLimitFilePath => Path.Combine(_appPaths.CachePath, "sd-image-limit.txt");

        private string MetadataLimitFilePath => Path.Combine(_appPaths.CachePath, "sd-metadata-limit.txt");

        /// <inheritdoc />
        public string Type => nameof(SchedulesDirect);

        private static List<string> GetScheduleRequestDates(DateTime startDateUtc, DateTime endDateUtc)
        {
            var dates = new List<string>();

            var start = new[] { startDateUtc, startDateUtc.ToLocalTime() }.Min().Date;
            var end = new[] { endDateUtc, endDateUtc.ToLocalTime() }.Max().Date;

            while (start <= end)
            {
                dates.Add(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                start = start.AddDays(1);
            }

            return dates;
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            // A failure must not come back as "this channel has no programs": GuideManager only
            // skips CleanDatabase when a channel throws, so an empty list deletes the guide.
            if (IsMetadataLimitActive())
            {
                throw new InvalidOperationException("The Schedules Direct daily request limit has been reached.");
            }

            ArgumentException.ThrowIfNullOrEmpty(channelId);

            // Normalize incoming input
            channelId = channelId.Replace(".json.schedulesdirect.org", string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('I');

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                throw new AuthenticationException("Could not authenticate with Schedules Direct");
            }

            if (!await IsSystemOnline(info, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Schedules Direct reports that the service is offline.");
            }

            var dates = GetScheduleRequestDates(startDateUtc, endDateUtc);
            _logger.LogInformation("Channel Station ID is: {ChannelID}", channelId);

            // Ask what changed before downloading anything; a day whose md5 still matches is
            // served from the cache and costs no schedule or program request.
            var (md5s, unavailableDates) = await GetScheduleMd5s(info, channelId, dates, cancellationToken).ConfigureAwait(false);

            var cachedDays = new List<SchedulesDirectCachedDay>();
            var datesToFetch = new List<string>();
            foreach (var date in dates)
            {
                if (unavailableDates.Contains(date))
                {
                    // The hash check already said the server has no schedule for this day, so
                    // requesting it would only earn the same error again.
                    continue;
                }

                md5s.TryGetValue(date, out var md5);
                var cached = await _scheduleCache.GetAsync(channelId, date, md5, cancellationToken).ConfigureAwait(false);
                if (cached?.Schedule is not null)
                {
                    cachedDays.Add(cached);
                }
                else if (!IsScheduleRetryPending(channelId, date))
                {
                    datesToFetch.Add(date);
                }
            }

            _logger.LogDebug(
                "Channel {ChannelID}: {CachedCount} of {TotalCount} days served from cache",
                channelId,
                cachedDays.Count,
                dates.Count);

            var freshDays = await DownloadSchedules(info, channelId, datesToFetch, token, cancellationToken).ConfigureAwait(false);
            _scheduleCache.Prune(channelId, dates);

            var allDays = cachedDays.Select(c => c.Schedule!).Concat(freshDays.Select(f => f.Schedule!)).ToList();
            if (allDays.Count == 0)
            {
                // Nothing came back. That is only genuinely "no programs" when the server said so
                // for every day; anything else is a failure, and reporting it as an empty list
                // would have the guide refresh delete the programs this channel already has.
                if (unavailableDates.Count == dates.Count)
                {
                    return [];
                }

                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Could not get any schedule for station {0}",
                        channelId));
            }

            var programDict = cachedDays.Concat(freshDays)
                .SelectMany(d => d.Programs)
                .Where(p => !string.IsNullOrEmpty(p.ProgramId))
                .DistinctBy(p => p.ProgramId)
                .ToDictionary(p => p.ProgramId, y => y);

            // Artwork uris are ephemeral and must not be cached, and artwork changes are not
            // reflected in the schedule md5 either, so the index is requested fresh - but only for
            // the programs whose images are actually going to be pre-cached.
            var imageProgramIds = allDays.SelectMany(d => d.Programs)
                .Where(s => WillBeImageCached(s)
                    && !string.IsNullOrEmpty(s.ProgramId)
                    && programDict.TryGetValue(s.ProgramId, out var detail)
                    && detail.HasImageArtwork)
                .Select(s => s.ProgramId)
                .Distinct()
                .ToList();

            var images = await GetImageForPrograms(info, imageProgramIds, cancellationToken).ConfigureAwait(false);

            var programsInfo = new List<ProgramInfo>();
            foreach (ProgramDto schedule in allDays.SelectMany(d => d.Programs))
            {
                if (string.IsNullOrEmpty(schedule.ProgramId)
                    || !programDict.TryGetValue(schedule.ProgramId, out var programDetail))
                {
                    continue;
                }

                // Only add images which will be pre-cached until we can implement dynamic token fetching
                if (WillBeImageCached(schedule))
                {
                    SetProgramImages(schedule.ProgramId, programDetail, images, token);
                }

                programsInfo.Add(GetProgram(channelId, schedule, programDetail));
            }

            return programsInfo;
        }

        private async Task<(Dictionary<string, string> Md5s, HashSet<string> UnavailableDates)> GetScheduleMd5s(
            ListingsProviderInfo info,
            string stationId,
            IReadOnlyList<string> dates,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var unavailable = new HashSet<string>(StringComparer.Ordinal);

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(token))
            {
                return (result, unavailable);
            }

            var requestList = new List<RequestScheduleForChannelDto>
            {
                new() { StationId = stationId, Date = dates }
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/schedules/md5");
            message.Content = JsonContent.Create(requestList, options: _jsonOptions);
            message.Headers.TryAddWithoutValidation("token", token);

            try
            {
                var response = await Request<Dictionary<string, Dictionary<string, ScheduleMd5Dto>>>(message, true, info, cancellationToken)
                    .ConfigureAwait(false);
                if (response is null || !response.TryGetValue(stationId, out var days))
                {
                    return (result, unavailable);
                }

                foreach (var (date, entry) in days)
                {
                    if (entry.Code != 0)
                    {
                        // Only a definitive "there is no such day" is worth skipping; a transient
                        // error still gets the normal request, which may well succeed.
                        if (IsDefinitiveScheduleError(entry.Code))
                        {
                            unavailable.Add(date);
                        }

                        LogEntryErrors([(entry.Code, date, entry.Message)], "schedule");
                    }
                    else if (!string.IsNullOrEmpty(entry.Md5))
                    {
                        result[date] = entry.Md5;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Without md5s every requested day is treated as changed, which is the old
                // behaviour. Nothing is marked unavailable, so no day is skipped on a failure.
                _logger.LogWarning(ex, "Unable to get schedule hashes for station {StationId}", stationId);
            }

            return (result, unavailable);
        }

        // Whether the code means the day will never be available, as opposed to a transient
        // failure that is worth another request.
        private static bool IsDefinitiveScheduleError(int code)
            => code is (int)SdErrorCode.ScheduleRangeExceeded
                or (int)SdErrorCode.ScheduleNotInLineup
                or (int)SdErrorCode.StationIdNotFound
                or (int)SdErrorCode.StationIdDeleted;

        private bool IsScheduleRetryPending(string stationId, string date)
        {
            var key = stationId + "|" + date;
            if (!_scheduleRetryTimes.TryGetValue(key, out var retryTime))
            {
                return false;
            }

            if (DateTime.UtcNow < retryTime)
            {
                _logger.LogDebug(
                    "Schedules Direct has queued {StationId} {Date}; not requesting it before {RetryTime}",
                    stationId,
                    date,
                    retryTime);
                return true;
            }

            _scheduleRetryTimes.TryRemove(key, out _);
            return false;
        }

        private static bool WillBeImageCached(ProgramDto schedule)
        {
            var endDate = schedule.AirDateTime?.AddSeconds(schedule.Duration);
            return endDate.HasValue && endDate.Value < DateTime.UtcNow.AddDays(GuideManager.MaxCacheDays);
        }

        private async Task<IReadOnlyList<SchedulesDirectCachedDay>> DownloadSchedules(
            ListingsProviderInfo info,
            string stationId,
            IReadOnlyList<string> dates,
            string token,
            CancellationToken cancellationToken)
        {
            if (dates.Count == 0)
            {
                return [];
            }

            var requestList = new List<RequestScheduleForChannelDto>
            {
                new() { StationId = stationId, Date = dates }
            };

            _logger.LogDebug("Request string for schedules is: {@RequestString}", requestList);

            using var options = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/schedules");
            options.Content = JsonContent.Create(requestList, options: _jsonOptions);
            options.Headers.TryAddWithoutValidation("token", token);

            // A station-wide failure such as SCHEDULE_QUEUED arrives as a bare object rather than
            // the usual array, so the shape has to be inspected before it is deserialized.
            var payload = await Request<JsonElement>(options, true, info, cancellationToken).ConfigureAwait(false);
            var dailySchedules = payload.ValueKind switch
            {
                JsonValueKind.Array => payload.Deserialize<IReadOnlyList<DayDto>>(_jsonOptions),
                JsonValueKind.Object => [payload.Deserialize<DayDto>(_jsonOptions)],
                _ => null
            };

            if (dailySchedules is null)
            {
                return [];
            }

            var errorDays = dailySchedules.Where(d => d.Code.HasValue).ToList();
            RecordQueuedSchedules(stationId, dates, errorDays);
            LogEntryErrors(errorDays.Select(d => (d.Code!.Value, d.StationId ?? stationId, d.Message ?? d.Response)), "schedule");
            var days = dailySchedules.Where(d => !d.Code.HasValue).ToList();

            _logger.LogDebug("Found {ScheduleCount} days on {ChannelID} ScheduleDirect", days.Count, stationId);

            var programIds = days.SelectMany(d => d.Programs.Select(s => s.ProgramId))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var programDetails = await GetProgramDetails(info, programIds, token, cancellationToken).ConfigureAwait(false);
            var detailsById = programDetails.ToDictionary(p => p.ProgramId, StringComparer.Ordinal);

            var result = new List<SchedulesDirectCachedDay>(days.Count);
            foreach (var day in days)
            {
                var dayPrograms = day.Programs
                    .Select(p => p.ProgramId)
                    .Where(id => !string.IsNullOrEmpty(id) && detailsById.ContainsKey(id))
                    .Distinct()
                    .Select(id => detailsById[id])
                    .ToList();

                var cachedDay = new SchedulesDirectCachedDay
                {
                    Md5 = day.Metadata?.Md5,
                    Schedule = day,
                    Programs = dayPrograms
                };

                result.Add(cachedDay);

                var date = day.Metadata?.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (date is not null)
                {
                    // Must stay ahead of SetProgramImages: it stamps ephemeral uris and a token
                    // that expires daily onto these programs, and neither may reach the cache.
                    await _scheduleCache.SetAsync(stationId, date, cachedDay, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        private void RecordQueuedSchedules(string stationId, IReadOnlyList<string> dates, IReadOnlyList<DayDto> errorDays)
        {
            // Dates roll out of the guide window without being asked for again, so entries that
            // are no longer holding anything back have to be swept here.
            var now = DateTime.UtcNow;
            foreach (var (key, value) in _scheduleRetryTimes)
            {
                if (value <= now)
                {
                    _scheduleRetryTimes.TryRemove(key, out _);
                }
            }

            foreach (var day in errorDays)
            {
                if (day.Code != (int)SdErrorCode.ScheduleQueued || day.RetryTime is not { } retryTime)
                {
                    continue;
                }

                // The response is station-wide and names no date, so it covers everything asked for.
                _logger.LogInformation(
                    "Schedules Direct has queued the schedule for station {StationId}; retrying after {RetryTime}",
                    day.StationId ?? stationId,
                    retryTime);

                foreach (var date in dates)
                {
                    _scheduleRetryTimes[(day.StationId ?? stationId) + "|" + date] = retryTime.ToUniversalTime();
                }
            }
        }

        private async Task<List<ProgramDetailsDto>> GetProgramDetails(
            ListingsProviderInfo info,
            IReadOnlyList<string> programIds,
            string token,
            CancellationToken cancellationToken)
        {
            var results = new List<ProgramDetailsDto>(programIds.Count);

            for (var i = 0; i < programIds.Count; i += MaxProgramsPerRequest)
            {
                var batch = programIds.Skip(i).Take(MaxProgramsPerRequest);

                using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/programs");
                message.Headers.TryAddWithoutValidation("token", token);
                message.Content = JsonContent.Create(batch, options: _jsonOptions);

                var batchResult = await Request<IReadOnlyList<ProgramDetailsDto>>(message, true, info, cancellationToken).ConfigureAwait(false);
                if (batchResult is null)
                {
                    continue;
                }

                LogEntryErrors(batchResult.Where(p => p.Code.HasValue).Select(p => (p.Code!.Value, p.ProgramId, p.Message)), "program");

                // Error entries carry no program data. A queued program (6001) is a soft failure,
                // so it is simply left out and picked up once the day's md5 changes again.
                results.AddRange(batchResult.Where(p => !p.Code.HasValue && !string.IsNullOrEmpty(p.ProgramId)));
            }

            return results.DistinctBy(p => p.ProgramId, StringComparer.Ordinal).ToList();
        }

        private void SetProgramImages(string programId, ProgramDetailsDto programEntry, IReadOnlyList<ShowImagesDto> images, string token)
        {
            var match = images.FirstOrDefault(i =>
                i.ProgramId is not null && programId.StartsWith(i.ProgramId, StringComparison.Ordinal));
            if (match is null)
            {
                return;
            }

            var allImages = match.Data;
            var imagesWithText = allImages.Where(i => string.Equals(i.Text, "yes", StringComparison.OrdinalIgnoreCase)).ToList();
            var imagesWithoutText = allImages.Where(i => string.Equals(i.Text, "no", StringComparison.OrdinalIgnoreCase)).ToList();

            const double DesiredAspect = 2.0 / 3;
            const double WideAspect = 16.0 / 9;

            programEntry.PrimaryImage = GetProgramImage(ApiUrl, imagesWithText, DesiredAspect, token) ??
                                        GetProgramImage(ApiUrl, allImages, DesiredAspect, token);

            programEntry.ThumbImage = GetProgramImage(ApiUrl, imagesWithText, WideAspect, token);

            // Don't supply the same image twice
            if (string.Equals(programEntry.PrimaryImage, programEntry.ThumbImage, StringComparison.Ordinal))
            {
                programEntry.ThumbImage = null;
            }

            programEntry.BackdropImage = GetProgramImage(ApiUrl, imagesWithoutText, WideAspect, token);
        }

        private void LogEntryErrors(IEnumerable<(int Code, string Id, string Message)> errors, string kind)
        {
            foreach (var (code, id, message) in errors)
            {
                _logger.LogWarning(
                    "Schedules Direct returned an error for {Kind} {Id}: code={Code}, message={Message}",
                    kind,
                    id,
                    code,
                    message);

                var sdCode = ToKnownErrorCode(code);
                if (sdCode.HasValue)
                {
                    ApplyErrorCode(sdCode.Value);
                }
            }
        }

        private static int GetSizeOrder(ImageDataDto image)
        {
            if (int.TryParse(image.Height, out int value))
            {
                return value;
            }

            return 0;
        }

        private static string GetChannelNumber(MapDto map)
        {
            var channelNumber = map.LogicalChannelNumber;

            if (string.IsNullOrWhiteSpace(channelNumber))
            {
                channelNumber = map.Channel;
            }

            if (string.IsNullOrWhiteSpace(channelNumber))
            {
                channelNumber = map.AtscMajor + "." + map.AtscMinor;
            }

            return channelNumber.TrimStart('0');
        }

        private static bool IsMovie(ProgramDetailsDto programInfo)
        {
            return string.Equals(programInfo.EntityType, "movie", StringComparison.OrdinalIgnoreCase);
        }

        private ProgramInfo GetProgram(string channelId, ProgramDto programInfo, ProgramDetailsDto details)
        {
            if (programInfo.AirDateTime is null)
            {
                return null;
            }

            var startAt = programInfo.AirDateTime.Value;
            var endAt = startAt.AddSeconds(programInfo.Duration);
            var audioType = ProgramAudio.Stereo;

            var programId = programInfo.ProgramId ?? string.Empty;

            string newID = programId + "T" + startAt.Ticks + "C" + channelId;

            if (programInfo.AudioProperties.Count != 0)
            {
                if (programInfo.AudioProperties.Contains("atmos", StringComparison.OrdinalIgnoreCase))
                {
                    audioType = ProgramAudio.Atmos;
                }
                else if (programInfo.AudioProperties.Contains("dd 5.1", StringComparison.OrdinalIgnoreCase))
                {
                    audioType = ProgramAudio.DolbyDigital;
                }
                else if (programInfo.AudioProperties.Contains("dd", StringComparison.OrdinalIgnoreCase))
                {
                    audioType = ProgramAudio.DolbyDigital;
                }
                else if (programInfo.AudioProperties.Contains("stereo", StringComparison.OrdinalIgnoreCase))
                {
                    audioType = ProgramAudio.Stereo;
                }
                else
                {
                    audioType = ProgramAudio.Mono;
                }
            }

            string episodeTitle = null;
            if (details.EpisodeTitle150 is not null)
            {
                episodeTitle = details.EpisodeTitle150;
            }

            var info = new ProgramInfo
            {
                ChannelId = channelId,
                Id = newID,
                StartDate = startAt,
                EndDate = endAt,
                Name = details.Titles[0].Title120 ?? "Unknown",
                OfficialRating = null,
                CommunityRating = null,
                EpisodeTitle = episodeTitle,
                Audio = audioType,
                IsRepeat = programInfo.New is null,
                IsSeries = string.Equals(details.EntityType, "episode", StringComparison.OrdinalIgnoreCase),
                ImageUrl = details.PrimaryImage,
                ThumbImageUrl = details.ThumbImage,
                IsKids = string.Equals(details.Audience, "children", StringComparison.OrdinalIgnoreCase),
                IsSports = string.Equals(details.EntityType, "sports", StringComparison.OrdinalIgnoreCase),
                IsMovie = IsMovie(details),
                Etag = programInfo.Md5,
                IsLive = string.Equals(programInfo.LiveTapeDelay, "live", StringComparison.OrdinalIgnoreCase),
                IsPremiere = programInfo.Premiere || (programInfo.IsPremiereOrFinale ?? string.Empty).Contains("premiere", StringComparison.OrdinalIgnoreCase)
            };

            var showId = programId;

            if (!info.IsSeries)
            {
                // It's also a series if it starts with SH
                info.IsSeries = showId.StartsWith("SH", StringComparison.OrdinalIgnoreCase) && showId.Length >= 14;
            }

            // According to SchedulesDirect, these are generic, unidentified episodes
            // SH005316560000
            var hasUniqueShowId = !showId.StartsWith("SH", StringComparison.OrdinalIgnoreCase) ||
                !showId.EndsWith("0000", StringComparison.OrdinalIgnoreCase);

            if (!hasUniqueShowId)
            {
                showId = newID;
            }

            info.ShowId = showId;

            if (programInfo.VideoProperties is not null)
            {
                info.IsHD = programInfo.VideoProperties.Contains("hdtv", StringComparison.OrdinalIgnoreCase);
                info.Is3D = programInfo.VideoProperties.Contains("3d", StringComparison.OrdinalIgnoreCase);
            }

            if (details.ContentRating is not null && details.ContentRating.Count > 0)
            {
                info.OfficialRating = details.ContentRating[0].Code.Replace("TV", "TV-", StringComparison.Ordinal)
                    .Replace("--", "-", StringComparison.Ordinal);

                var invalid = new[] { "N/A", "Approved", "Not Rated", "Passed" };
                if (invalid.Contains(info.OfficialRating, StringComparison.OrdinalIgnoreCase))
                {
                    info.OfficialRating = null;
                }
            }

            if (details.Descriptions is not null)
            {
                if (details.Descriptions.Description1000 is not null && details.Descriptions.Description1000.Count > 0)
                {
                    info.Overview = details.Descriptions.Description1000[0].Description;
                }
                else if (details.Descriptions.Description100 is not null && details.Descriptions.Description100.Count > 0)
                {
                    info.Overview = details.Descriptions.Description100[0].Description;
                }
            }

            if (info.IsSeries)
            {
                info.SeriesId = programId.Substring(0, 10);

                info.SeriesProviderIds[MetadataProvider.Zap2It.ToString()] = info.SeriesId;

                if (details.Metadata is not null)
                {
                    foreach (var metadataProgram in details.Metadata)
                    {
                        var gracenote = metadataProgram.Gracenote;
                        if (gracenote is not null)
                        {
                            info.SeasonNumber = gracenote.Season;

                            if (gracenote.Episode > 0)
                            {
                                info.EpisodeNumber = gracenote.Episode;
                            }

                            break;
                        }
                    }
                }
            }

            if (details.OriginalAirDate is not null)
            {
                info.OriginalAirDate = details.OriginalAirDate;
                info.ProductionYear = info.OriginalAirDate.Value.Year;
            }

            if (details.Movie is not null)
            {
                if (!string.IsNullOrEmpty(details.Movie.Year)
                    && int.TryParse(details.Movie.Year, out int year))
                {
                    info.ProductionYear = year;
                }
            }

            if (details.Genres is not null)
            {
                info.Genres = details.Genres.Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                info.IsNews = details.Genres.Contains("news", StringComparison.OrdinalIgnoreCase);

                if (info.Genres.Contains("children", StringComparison.OrdinalIgnoreCase))
                {
                    info.IsKids = true;
                }
            }

            return info;
        }

        private string GetProgramImage(string apiUrl, IEnumerable<ImageDataDto> images, double desiredAspect, string token)
        {
            // A uri Schedules Direct has already rejected must never be offered again; repeating
            // the request is what trips MAX_IMAGE_INVALID_URI_ERRORS and blocks the account.
            var match = images
                .Where(i => !string.IsNullOrWhiteSpace(i.Uri) && !_rejectedImageUris.ContainsKey(GetImageKey(i.Uri)))
                .OrderBy(i => Math.Abs(desiredAspect - GetAspectRatio(i)))
                .ThenByDescending(i => GetSizeOrder(i))
                .FirstOrDefault();

            if (match is null)
            {
                return null;
            }

            var uri = match.Uri;

            if (uri.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                return uri;
            }

            return apiUrl + "/image/" + uri + "?token=" + token;
        }

        private static string GetImageKey(string uriOrUrl)
        {
            var value = uriOrUrl;

            var imageSegment = value.IndexOf("/image/", StringComparison.OrdinalIgnoreCase);
            if (imageSegment >= 0)
            {
                value = value[(imageSegment + 7)..];
            }

            var query = value.IndexOf('?', StringComparison.Ordinal);
            return query < 0 ? value : value[..query];
        }

        private static double GetAspectRatio(ImageDataDto i)
        {
            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(i.Width))
            {
                _ = int.TryParse(i.Width, out width);
            }

            if (!string.IsNullOrWhiteSpace(i.Height))
            {
                _ = int.TryParse(i.Height, out height);
            }

            if (height == 0 || width == 0)
            {
                return 0;
            }

            double result = width;
            result /= height;
            return result;
        }

        private async Task<IReadOnlyList<ShowImagesDto>> GetImageForPrograms(
            ListingsProviderInfo info,
            IReadOnlyList<string> programIds,
            CancellationToken cancellationToken)
        {
            if (IsImageDailyLimitActive())
            {
                return [];
            }

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token) || programIds.Count == 0)
            {
                return [];
            }

            // SD API accepts max 500 program IDs per request
            const int BatchSize = 500;
            var results = new List<ShowImagesDto>();
            for (int i = 0; i < programIds.Count; i += BatchSize)
            {
                // The daily image limit may be surfaced mid-batch.
                if (IsImageDailyLimitActive())
                {
                    break;
                }

                var batch = programIds.Skip(i).Take(BatchSize);

                using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/metadata/programs/");
                message.Headers.TryAddWithoutValidation("token", token);
                message.Content = JsonContent.Create(batch, options: _jsonOptions);

                try
                {
                    var batchResult = await Request<IReadOnlyList<ShowImagesDto>>(message, true, info, cancellationToken).ConfigureAwait(false);
                    if (batchResult is not null)
                    {
                        foreach (var entry in batchResult)
                        {
                            if (entry.Code.HasValue)
                            {
                                _logger.LogWarning(
                                    "Schedules Direct returned error for program {ProgramId}: code={Code}, message={Message}",
                                    entry.ProgramId,
                                    entry.Code,
                                    entry.Message);

                                // The image download limit can be reported per-entry inside an
                                // otherwise successful (HTTP 200) response when the limit is hit
                                // mid-batch. Back off so we stop requesting images until SD resets.
                                if (entry.Code is (int)SdErrorCode.MaxImageDownloads or (int)SdErrorCode.MaxImageDownloadsTrial)
                                {
                                    _logger.LogError(
                                        "Schedules Direct image download limit hit (code {Code}). Disabling image acquisition until SD reset.",
                                        entry.Code);
                                    SetImageLimitHit();
                                }

                                continue;
                            }

                            results.Add(entry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting image info from schedules direct");
                }
            }

            return results;
        }

        public async Task<List<NameIdPair>> GetHeadends(ListingsProviderInfo info, string country, string location, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new AuthenticationException("Could not authenticate with Schedules Direct");
            }

            // The spec puts the status check ahead of all processing, and an empty lineup list is
            // indistinguishable from "nothing for this postal code", so this has to surface.
            if (!await IsSystemOnline(info, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Schedules Direct reports that the service is offline. Try again later.");
            }

            var lineups = new List<NameIdPair>();

            using var options = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/headends?country=" + country + "&postalcode=" + location);
            options.Headers.TryAddWithoutValidation("token", token);

            var root = await Request<IReadOnlyList<HeadendsDto>>(options, false, info, cancellationToken).ConfigureAwait(false);
            foreach (HeadendsDto headend in root ?? [])
            {
                foreach (LineupDto lineup in headend.Lineups ?? [])
                {
                    lineups.Add(new NameIdPair
                    {
                        Name = string.IsNullOrWhiteSpace(lineup.Name) ? lineup.Lineup : lineup.Name,
                        Id = string.IsNullOrWhiteSpace(lineup.Lineup) ? lineup.Uri?.Split('/')[^1] : lineup.Lineup
                    });
                }
            }

            if (lineups.Count == 0)
            {
                _logger.LogWarning(
                    "Schedules Direct has no lineups for country {Country} and postal code {PostalCode}",
                    country,
                    location);
            }

            return lineups;
        }

        private void ResetErrorState(ListingsProviderInfo info)
        {
            _accountError = false;
            Interlocked.Exchange(ref _lastErrorResponseTicks, 0);
            Interlocked.Exchange(ref _offlineUntilTicks, 0);
            _status = null;

            // Only the account being saved is retried, the tokens of the other accounts stay valid.
            if (!string.IsNullOrWhiteSpace(info.Username))
            {
                _tokens.TryRemove(info.Username, out _);
            }
        }

        private async Task<string> GetToken(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            var username = info.Username;

            // Reset the token if there's no username
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var password = info.Password;
            if (string.IsNullOrEmpty(password))
            {
                return null;
            }

            // Account error — SD stays disabled until the provider is saved again or the server restarts.
            if (_accountError)
            {
                _logger.LogWarning("Skipping Schedules Direct request because of an earlier account error. Save the listings provider again to retry.");

                return null;
            }

            // Avoid hammering SD after transient login failures (e.g. max attempts / temporary lockout)
            if ((DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastErrorResponseTicks), DateTimeKind.Utc)).TotalMinutes < 30)
            {
                _logger.LogWarning("Skipping Schedules Direct request because of a recent login failure. Retrying no earlier than 30 minutes after it.");

                return null;
            }

            if (!_tokens.TryGetValue(username, out NameValuePair savedToken))
            {
                savedToken = new NameValuePair();
                _tokens.TryAdd(username, savedToken);
            }

            if (!string.IsNullOrEmpty(savedToken.Name)
                && long.TryParse(savedToken.Value, CultureInfo.InvariantCulture, out long ticks))
            {
                // If it's under 24 hours old we can still use it
                if (DateTime.UtcNow.Ticks - ticks < TimeSpan.FromHours(20).Ticks)
                {
                    return savedToken.Name;
                }
            }

            using (await _tokenLock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var result = await GetTokenInternal(username, password, cancellationToken).ConfigureAwait(false);
                    savedToken.Name = result;
                    savedToken.Value = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    // For 4xx errors not already handled by Request<T>'s SD code logic
                    // (e.g. unparseable response from the /token endpoint), apply a
                    // temporary backoff to avoid hammering SD.
                    if (!_accountError
                        && ex.StatusCode.HasValue
                        && (int)ex.StatusCode.Value >= 400
                        && (int)ex.StatusCode.Value < 500)
                    {
                        _tokens.Clear();
                        Interlocked.Exchange(ref _lastErrorResponseTicks, DateTime.UtcNow.Ticks);
                    }

                    throw;
                }
            }
        }

        private async Task<bool> IsSystemOnline(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            if (DateTime.UtcNow.Ticks < Interlocked.Read(ref _offlineUntilTicks))
            {
                _logger.LogWarning("Skipping Schedules Direct request, the service reported itself offline");
                return false;
            }

            var status = await GetStatus(info, cancellationToken).ConfigureAwait(false);
            if (status is null)
            {
                // A failed status check is not a reason to skip the refresh on its own.
                return true;
            }

            // The spec does not define the order of the entries, so any of them can be the one
            // that takes the service down.
            var offline = status.SystemStatus.FirstOrDefault(
                i => string.Equals(i.Status, "Offline", StringComparison.OrdinalIgnoreCase));
            if (offline is not null)
            {
                _logger.LogWarning(
                    "Schedules Direct reports the service is offline ({Message}). Not retrying for {Minutes} minutes.",
                    offline.Message,
                    OfflineBackoffMinutes);
                Interlocked.Exchange(ref _offlineUntilTicks, DateTime.UtcNow.AddMinutes(OfflineBackoffMinutes).Ticks);
                return false;
            }

            if (status.Account?.Expires is { } expires && expires < DateTime.UtcNow.AddDays(7))
            {
                _logger.LogWarning("The Schedules Direct subscription expires on {Expires}", expires);
            }

            return true;
        }

        private async Task<StatusDto> GetStatus(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            if (TryGetCachedStatus(out var cached))
            {
                return cached;
            }

            using (await _statusLock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                // Whoever held the lock may have just refreshed it, and a guide refresh asking
                // once per channel must not turn into one request per channel.
                if (TryGetCachedStatus(out cached))
                {
                    return cached;
                }

                StatusDto status = null;
                try
                {
                    var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

                    using var message = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/status");
                    message.Headers.TryAddWithoutValidation("token", token);

                    status = await Request<StatusDto>(message, true, info, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or AuthenticationException)
                {
                    _logger.LogWarning(ex, "Unable to get the Schedules Direct service status");
                }

                // A failed check still counts as checked, or every channel retries it.
                _status = new StatusCacheEntry(DateTime.UtcNow, status);
                return status;
            }
        }

        private bool TryGetCachedStatus(out StatusDto status)
        {
            // One reference, so the time of the check and what it found can never disagree.
            var entry = _status;
            status = entry?.Status;
            return entry is not null && (DateTime.UtcNow - entry.CheckedAt).TotalMinutes < StatusCacheMinutes;
        }

        private bool ApplyErrorCode(SdErrorCode sdCode)
        {
            switch (sdCode)
            {
                case SdErrorCode.AccountExpired:
                case SdErrorCode.InvalidHash:
                case SdErrorCode.InvalidUser:
                case SdErrorCode.AccountLocked:
                case SdErrorCode.AppLocked:
                case SdErrorCode.AccountInactive:
                    // Permanent account errors — disable SD for this server lifetime.
                    _logger.LogError("Schedules Direct account error (code {SdCode}). Disabling SD until server restart.", sdCode);
                    _tokens.Clear();
                    _accountError = true;
                    return true;

                case SdErrorCode.ServiceOffline:
                    // The spec requires staying away for an hour once the service reports offline.
                    _logger.LogError("Schedules Direct is offline (code {SdCode}). Not retrying for {Minutes} minutes.", sdCode, OfflineBackoffMinutes);
                    _tokens.Clear();
                    Interlocked.Exchange(ref _offlineUntilTicks, DateTime.UtcNow.AddMinutes(OfflineBackoffMinutes).Ticks);
                    return true;

                case SdErrorCode.ServiceBusy:
                case SdErrorCode.AccountTempLock:
                    // Transient login errors — back off for 30 minutes, then allow retry.
                    _logger.LogError("Schedules Direct transient error (code {SdCode}). Backing off for 30 minutes.", sdCode);
                    _tokens.Clear();
                    Interlocked.Exchange(ref _lastErrorResponseTicks, DateTime.UtcNow.Ticks);
                    return true;

                case SdErrorCode.MaxLoginAttempts:
                case SdErrorCode.MaxIPAttempts:
                    // These count logins, so continuing to ask for a token is what earned them.
                    // The user has to contact SD support, so nothing is retried automatically.
                    _logger.LogError(
                        "Schedules Direct account limit error (code {SdCode}). Disabling SD until server restart; the user has to contact Schedules Direct support.",
                        sdCode);
                    _tokens.Clear();
                    _accountError = true;
                    SetImageLimitHit();
                    SetMetadataLimitHit();
                    return true;

                case SdErrorCode.MaxImageDownloads:
                case SdErrorCode.MaxImageDownloadsTrial:
                case SdErrorCode.MaxInvalidImages:
                    // Stop image requests until SD resets at 00:00 UTC.
                    _logger.LogError("Schedules Direct image download limit hit (code {SdCode}). Disabling image acquisition until SD reset.", sdCode);
                    SetImageLimitHit();
                    return true;

                case SdErrorCode.MaxLineupChanges:
                    // Only blocks further lineup edits; guide data is unaffected.
                    _logger.LogError("Schedules Direct lineup change limit reached (code {SdCode}). Lineup edits are rejected until SD reset.", sdCode);
                    return true;

                default:
                    return false;
            }
        }

        private async Task<T> Request<T>(
            HttpRequestMessage message,
            bool enableRetry,
            ListingsProviderInfo providerInfo,
            CancellationToken cancellationToken,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(message, completionOption, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var errorCode = TryGetErrorCode(responseBody);
            var sdCode = ToKnownErrorCode(errorCode);

            _logger.LogError(
                "Request to {Url} failed with HTTP {StatusCode}, SD code {SdCode}: {Response}",
                message.RequestUri,
                (int)response.StatusCode,
                sdCode?.ToString() ?? errorCode?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                responseBody);

            if (sdCode.HasValue && ApplyErrorCode(sdCode.Value))
            {
                // Handled by the error code.
            }
            else if (enableRetry
                && (int)response.StatusCode < 500
                && (sdCode == SdErrorCode.TokenExpired || (response.StatusCode == HttpStatusCode.Forbidden && errorCode is null)))
            {
                // Token expired — clear tokens and retry with a fresh token. Also retry on 403
                // carrying no code at all; a code we simply do not know is not an auth failure.
                _tokens.Clear();
                using var retryMessage = new HttpRequestMessage(message.Method, message.RequestUri);
                retryMessage.Content = message.Content;
                retryMessage.Headers.TryAddWithoutValidation(
                    "token",
                    await GetToken(providerInfo, cancellationToken).ConfigureAwait(false));

                return await Request<T>(retryMessage, false, providerInfo, cancellationToken).ConfigureAwait(false);
            }

            throw new HttpRequestException(
                string.Format(CultureInfo.InvariantCulture, "Request failed: {0}", response.ReasonPhrase),
                null,
                response.StatusCode);
        }

        private async Task<string> GetTokenInternal(
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            using var options = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/token");
#pragma warning disable CA5350 // SchedulesDirect is always SHA1.
            var hashedPasswordBytes = SHA1.HashData(Encoding.ASCII.GetBytes(password));
#pragma warning restore CA5350
            string hashedPassword = Convert.ToHexStringLower(hashedPasswordBytes);
            options.Content = new StringContent("{\"username\":\"" + username + "\",\"password\":\"" + hashedPassword + "\"}", Encoding.UTF8, MediaTypeNames.Application.Json);

            var root = await Request<TokenDto>(options, false, null, cancellationToken).ConfigureAwait(false);
            if (string.Equals(root?.Message, "OK", StringComparison.Ordinal))
            {
                _logger.LogInformation("Authenticated with Schedules Direct token: {Token}", root.Token);
                return root.Token;
            }

            // A rejected login can still arrive as HTTP 200 with an error code in the body, so the
            // code has to be acted on here as well or we keep retrying a disabled account.
            var tokenCode = ToKnownErrorCode(root?.Code);
            if (tokenCode.HasValue)
            {
                ApplyErrorCode(tokenCode.Value);
            }

            throw new AuthenticationException("Could not authenticate with Schedules Direct Error: " + (root?.Message ?? "empty response"));
        }

        private async Task AddLineupToAccount(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            ArgumentException.ThrowIfNullOrEmpty(token);
            ArgumentException.ThrowIfNullOrEmpty(info.ListingsId);

            _logger.LogInformation("Adding new lineup {Id}", info.ListingsId);

            using var message = new HttpRequestMessage(HttpMethod.Put, ApiUrl + "/lineups/" + info.ListingsId);
            message.Headers.TryAddWithoutValidation("token", token);

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorCode = TryGetErrorCode(responseBody);
                var sdCode = ToKnownErrorCode(errorCode);
                if (sdCode.HasValue)
                {
                    ApplyErrorCode(sdCode.Value);
                }

                _logger.LogError(
                    "Error adding lineup {Id} to account, SD code {SdCode}: {Response}",
                    info.ListingsId,
                    sdCode?.ToString() ?? errorCode?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                    responseBody);

                // Reporting success here leaves the user with a listings provider that silently
                // has no lineup, so the failure has to surface.
                throw new HttpRequestException(
                    string.Format(CultureInfo.InvariantCulture, "Could not add lineup {0} to the Schedules Direct account", info.ListingsId),
                    null,
                    response.StatusCode);
            }
        }

        private async Task<bool> HasLineup(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(info.ListingsId);

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            ArgumentException.ThrowIfNullOrEmpty(token);

            _logger.LogInformation("Headends on account ");

            using var options = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/lineups");
            options.Headers.TryAddWithoutValidation("token", token);

            try
            {
                var root = await Request<LineupsDto>(options, false, null, cancellationToken).ConfigureAwait(false);
                return root?.Lineups.Any(i => string.Equals(info.ListingsId, i.Lineup, StringComparison.OrdinalIgnoreCase)) ?? false;
            }
            catch (HttpRequestException ex)
            {
                // SchedulesDirect returns 400 if no lineups are configured.
                if (ex.StatusCode is HttpStatusCode.BadRequest)
                {
                    return false;
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Stream> GetAvailableCountries(CancellationToken cancellationToken)
        {
            if (_countriesCache is not null)
            {
                return new MemoryStream(_countriesCache, writable: false);
            }

            var cachePath = Path.Combine(_appPaths.CachePath, "sd-countries.json");

            if (File.Exists(cachePath)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < TimeSpan.FromDays(CountryCacheDays))
            {
                try
                {
                    _countriesCache = await File.ReadAllBytesAsync(cachePath, cancellationToken).ConfigureAwait(false);
                    return new MemoryStream(_countriesCache, writable: false);
                }
                catch (IOException)
                {
                    // Corrupt or unreadable — delete and re-fetch.
                    TryDeleteFile(cachePath);
                }
            }

            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            using var response = await client.GetAsync(new Uri(ApiUrl + "/available/countries"), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken).ConfigureAwait(false);

            _countriesCache = bytes;
            return new MemoryStream(bytes, writable: false);
        }

        private static DateOnly? LoadDailyLimitDate(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var text = File.ReadAllText(path).Trim();
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                {
                    var dateOnly = DateOnly.FromDateTime(date);
                    if (dateOnly < DateOnly.FromDateTime(DateTime.UtcNow))
                    {
                        // Expired — clean up.
                        File.Delete(path);
                        return null;
                    }

                    return dateOnly;
                }
            }
            catch (IOException)
            {
                // Corrupt or unreadable — delete and reset.
                TryDeleteFile(path);
            }

            return null;
        }

        /// <inheritdoc />
        public bool CanDownloadImage(string imageUrl)
            => !IsSchedulesDirectUrl(imageUrl)
                || (!IsImageDailyLimitActive() && !_rejectedImageUris.ContainsKey(GetImageKey(imageUrl)));

        /// <inheritdoc />
        public void ReportImageDownloadSuccess(string imageUrl)
        {
            if (IsSchedulesDirectUrl(imageUrl))
            {
                Interlocked.Exchange(ref _consecutiveImageFailures, 0);
            }
        }

        /// <inheritdoc />
        public ImageDownloadFailureAction ReportImageDownloadFailure(string imageUrl, HttpStatusCode? statusCode, string responseBody)
        {
            if (!IsSchedulesDirectUrl(imageUrl))
            {
                return ImageDownloadFailureAction.None;
            }

            var errorCode = TryGetErrorCode(responseBody);
            if (errorCode is null)
            {
                // No readable code, so stop after a short streak of unexplained rejections rather
                // than keep retrying an account that may already be refusing us.
                if (Interlocked.Increment(ref _consecutiveImageFailures) == MaxConsecutiveImageFailures)
                {
                    _logger.LogError(
                        "Schedules Direct image downloads keep failing without a readable error code (last status {StatusCode}). Disabling image acquisition until SD reset.",
                        statusCode);
                    SetImageLimitHit();
                }

                return ImageDownloadFailureAction.None;
            }

            Interlocked.Exchange(ref _consecutiveImageFailures, 0);

            var sdCode = ToKnownErrorCode(errorCode);
            if (sdCode is null)
            {
                return ImageDownloadFailureAction.None;
            }

            // SD reports an exhausted image quota with an HTTP 200 body, so the code decides.
            ApplyErrorCode(sdCode.Value);

            switch (sdCode.Value)
            {
                // The uri is gone. Remembering it is what keeps the next refresh from asking for
                // it again and walking the account into MAX_IMAGE_INVALID_URI_ERRORS.
                case SdErrorCode.ImageNotFound:
                    RejectImageUri(imageUrl);
                    return ImageDownloadFailureAction.RemoveImage;

                // The account is already being warned about invalid uris; this url is not
                // necessarily one of them, so drop it for now but do not blacklist it.
                case SdErrorCode.MaxInvalidImages:
                    return ImageDownloadFailureAction.RemoveImage;

                // The token baked into the url is stale; the next refresh mints a fresh one.
                case SdErrorCode.TokenMissing:
                case SdErrorCode.TokenInvalid:
                case SdErrorCode.TokenExpired:
                    _tokens.Clear();
                    return ImageDownloadFailureAction.RemoveImage;

                default:
                    return ImageDownloadFailureAction.None;
            }
        }

        private void RejectImageUri(string imageUrl)
        {
            var key = GetImageKey(imageUrl);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            // Stop learning rather than forgetting: clearing here would re-offer every known-bad
            // uri at once, which is the burst that trips MAX_IMAGE_INVALID_URI_ERRORS.
            if (_rejectedImageUris.Count < MaxRejectedImageUris)
            {
                _rejectedImageUris.TryAdd(key, 0);
            }
        }

        private static bool IsSchedulesDirectUrl(string url)
            => url is not null && url.Contains("schedulesdirect", StringComparison.OrdinalIgnoreCase);

        private static int? TryGetErrorCode(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("code", out var codeProp)
                    && codeProp.TryGetInt32(out var parsedCode))
                {
                    return parsedCode;
                }
            }
            catch (JsonException)
            {
                // Not an SD error payload.
            }

            return null;
        }

        private static SdErrorCode? ToKnownErrorCode(int? code)
            => code.HasValue && Enum.IsDefined((SdErrorCode)code.Value) ? (SdErrorCode)code.Value : null;

        private bool IsImageDailyLimitActive()
        {
            if (!_imageLimitHitDate.HasValue)
            {
                return false;
            }

            if (_imageLimitHitDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _imageLimitHitDate = null;
                Interlocked.Exchange(ref _consecutiveImageFailures, 0);
                TryDeleteFile(ImageLimitFilePath);
                return false;
            }

            return true;
        }

        private bool IsMetadataLimitActive()
        {
            if (!_metadataLimitHitDate.HasValue)
            {
                return false;
            }

            if (_metadataLimitHitDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _metadataLimitHitDate = null;
                TryDeleteFile(MetadataLimitFilePath);
                return false;
            }

            return true;
        }

        private void SetImageLimitHit()
        {
            _imageLimitHitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            PersistDailyLimitFile(ImageLimitFilePath);
        }

        private void SetMetadataLimitHit()
        {
            _metadataLimitHitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            PersistDailyLimitFile(MetadataLimitFilePath);
        }

        private void PersistDailyLimitFile(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to persist SD daily limit to {Path}", filePath);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Best effort.
            }
        }

        public async Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            ResetErrorState(info);

            if (validateLogin)
            {
                ArgumentException.ThrowIfNullOrEmpty(info.Username);
                ArgumentException.ThrowIfNullOrEmpty(info.Password);

                var token = await GetToken(info, CancellationToken.None).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new AuthenticationException("Could not authenticate with Schedules Direct");
                }
            }

            if (!await IsSystemOnline(info, CancellationToken.None).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Schedules Direct reports that the service is offline. Try again later.");
            }

            if (validateListings)
            {
                ArgumentException.ThrowIfNullOrEmpty(info.ListingsId);

                var hasLineup = await HasLineup(info, CancellationToken.None).ConfigureAwait(false);

                if (!hasLineup)
                {
                    await AddLineupToAccount(info, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        public Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            return GetHeadends(info, country, location, CancellationToken.None);
        }

        public async Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            var listingsId = info.ListingsId;
            if (string.IsNullOrEmpty(listingsId))
            {
                return [];
            }

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                return [];
            }

            if (!await IsSystemOnline(info, cancellationToken).ConfigureAwait(false))
            {
                return [];
            }

            // Stations that have left every lineup would otherwise keep their schedules forever.
            _scheduleCache.PruneStale(TimeSpan.FromDays(ScheduleCacheMaxAgeDays));

            var root = await GetLineup(info, listingsId, cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return [];
            }

            _logger.LogInformation("Found {ChannelCount} channels on the lineup on ScheduleDirect", root.Map.Count);
            _logger.LogInformation("Mapping Stations to Channel");

            var allStations = root.Stations;

            var map = root.Map;
            var list = new List<ChannelInfo>(map.Count);
            foreach (var channel in map)
            {
                var channelNumber = GetChannelNumber(channel);

                var stationIndex = allStations.FindIndex(item => string.Equals(item.StationId, channel.StationId, StringComparison.OrdinalIgnoreCase));
                var station = stationIndex == -1
                    ? new StationDto { StationId = channel.StationId }
                    : allStations[stationIndex];

                var channelInfo = new ChannelInfo
                {
                    Id = station.StationId,
                    CallSign = station.Callsign,
                    Number = channelNumber,
                    Name = string.IsNullOrWhiteSpace(station.Name) ? channelNumber : station.Name
                };

                if (station.Logo is not null)
                {
                    channelInfo.ImageUrl = station.Logo.Url;
                }

                list.Add(channelInfo);
            }

            return list;
        }

        private async Task<ChannelDto> GetLineup(ListingsProviderInfo info, string listingsId, CancellationToken cancellationToken)
        {
            var status = await GetStatus(info, cancellationToken).ConfigureAwait(false);
            var lineup = status?.Lineups.FirstOrDefault(
                i => string.Equals(i.Lineup ?? i.Id, listingsId, StringComparison.OrdinalIgnoreCase));

            if (lineup?.IsDeleted == true)
            {
                _logger.LogWarning(
                    "The Schedules Direct lineup {ListingsId} has been deleted at the headend. Pick a new lineup for this listings provider.",
                    listingsId);
            }

            var modified = lineup?.Modified;
            if (modified.HasValue)
            {
                var cached = await _lineupCache.GetAsync(listingsId, modified.Value, cancellationToken).ConfigureAwait(false);
                if (cached is not null)
                {
                    _logger.LogDebug("Serving the Schedules Direct lineup {ListingsId} from cache", listingsId);
                    return cached;
                }
            }

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            using var options = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/lineups/" + listingsId);
            options.Headers.TryAddWithoutValidation("token", token);

            var root = await Request<ChannelDto>(options, true, info, cancellationToken).ConfigureAwait(false);
            if (root is not null && modified.HasValue)
            {
                await _lineupCache.SetAsync(listingsId, modified.Value, root, cancellationToken).ConfigureAwait(false);
            }

            return root;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _tokenLock?.Dispose();
                _statusLock?.Dispose();
            }

            _disposed = true;
        }

        private sealed record StatusCacheEntry(DateTime CheckedAt, StatusDto Status);
    }
}
