#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings
{
    public class SchedulesDirect : IListingsProvider, IDisposable
    {
        private const string ApiUrl = "https://json.schedulesdirect.org/20141201";

        private readonly ILogger<SchedulesDirect> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AsyncNonKeyedLocker _tokenLock = new(1);

        private readonly ConcurrentDictionary<string, NameValuePair> _tokens = new();
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private DateTime _lastErrorResponse;
        private bool _disposed = false;

        public SchedulesDirect(
            ILogger<SchedulesDirect> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public string Name => "Schedules Direct";

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
            ArgumentException.ThrowIfNullOrEmpty(channelId);

            // Normalize incoming input
            channelId = channelId.Replace(".json.schedulesdirect.org", string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('I');

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("SchedulesDirect token is empty, returning empty program list");

                return [];
            }

            var dates = GetScheduleRequestDates(startDateUtc, endDateUtc);

            _logger.LogInformation("Channel Station ID is: {ChannelID}", channelId);
            var requestList = new List<RequestScheduleForChannelDto>()
                {
                    new()
                    {
                        StationId = channelId,
                        Date = dates
                    }
                };

            _logger.LogDebug("Request string for schedules is: {@RequestString}", requestList);

            using var options = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/schedules");
            options.Content = JsonContent.Create(requestList, options: _jsonOptions);
            options.Headers.TryAddWithoutValidation("token", token);
            var dailySchedules = await Request<IReadOnlyList<DayDto>>(options, true, info, cancellationToken).ConfigureAwait(false);
            if (dailySchedules is null)
            {
                return [];
            }

            _logger.LogDebug("Found {ScheduleCount} programs on {ChannelID} ScheduleDirect", dailySchedules.Count, channelId);

            using var programRequestOptions = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/programs");
            programRequestOptions.Headers.TryAddWithoutValidation("token", token);

            var programIds = dailySchedules.SelectMany(d => d.Programs.Select(s => s.ProgramId)).Distinct();
            programRequestOptions.Content = JsonContent.Create(programIds, options: _jsonOptions);

            var programDetails = await Request<IReadOnlyList<ProgramDetailsDto>>(programRequestOptions, true, info, cancellationToken).ConfigureAwait(false);
            if (programDetails is null)
            {
                return [];
            }

            var programDict = programDetails.ToDictionary(p => p.ProgramId, y => y);

            var programIdsWithImages = programDetails
                .Where(p => p.HasImageArtwork)
                .Select(p => p.ProgramId)
                .ToList();

            var images = await GetImageForPrograms(info, programIdsWithImages, cancellationToken).ConfigureAwait(false);

            var programsInfo = new List<ProgramInfo>();
            foreach (ProgramDto schedule in dailySchedules.SelectMany(d => d.Programs))
            {
                if (string.IsNullOrEmpty(schedule.ProgramId))
                {
                    continue;
                }

                // Only add images which will be pre-cached until we can implement dynamic token fetching
                var endDate = schedule.AirDateTime?.AddSeconds(schedule.Duration);
                var willBeCached = endDate.HasValue && endDate.Value < DateTime.UtcNow.AddDays(GuideManager.MaxCacheDays);
                if (willBeCached && images is not null)
                {
                    var imageIndex = images.FindIndex(i => i.ProgramId == schedule.ProgramId[..10]);
                    if (imageIndex > -1)
                    {
                        var programEntry = programDict[schedule.ProgramId];

                        var allImages = images[imageIndex].Data;
                        var imagesWithText = allImages.Where(i => string.Equals(i.Text, "yes", StringComparison.OrdinalIgnoreCase)).ToList();
                        var imagesWithoutText = allImages.Where(i => string.Equals(i.Text, "no", StringComparison.OrdinalIgnoreCase)).ToList();

                        const double DesiredAspect = 2.0 / 3;

                        programEntry.PrimaryImage = GetProgramImage(ApiUrl, imagesWithText, DesiredAspect, token) ??
                                                    GetProgramImage(ApiUrl, allImages, DesiredAspect, token);

                        const double WideAspect = 16.0 / 9;

                        programEntry.ThumbImage = GetProgramImage(ApiUrl, imagesWithText, WideAspect, token);

                        // Don't supply the same image twice
                        if (string.Equals(programEntry.PrimaryImage, programEntry.ThumbImage, StringComparison.Ordinal))
                        {
                            programEntry.ThumbImage = null;
                        }

                        programEntry.BackdropImage = GetProgramImage(ApiUrl, imagesWithoutText, WideAspect, token);

                        // programEntry.bannerImage = GetProgramImage(ApiUrl, data, "Banner", false) ??
                        //    GetProgramImage(ApiUrl, data, "Banner-L1", false) ??
                        //    GetProgramImage(ApiUrl, data, "Banner-LO", false) ??
                        //    GetProgramImage(ApiUrl, data, "Banner-LOT", false);
                    }
                }

                programsInfo.Add(GetProgram(channelId, schedule, programDict[schedule.ProgramId]));
            }

            return programsInfo;
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
                // IsNew = programInfo.@new ?? false,
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

        private static string GetProgramImage(string apiUrl, IEnumerable<ImageDataDto> images, double desiredAspect, string token)
        {
            var match = images
                .OrderBy(i => Math.Abs(desiredAspect - GetAspectRatio(i)))
                .ThenByDescending(i => GetSizeOrder(i))
                .FirstOrDefault();

            if (match is null)
            {
                return null;
            }

            var uri = match.Uri;

            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            if (uri.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                return uri;
            }

            return apiUrl + "/image/" + uri + "?token=" + token;
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
            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (programIds.Count == 0)
            {
                return [];
            }

            StringBuilder str = new StringBuilder("[", 1 + (programIds.Count * 13));
            foreach (var i in programIds)
            {
                str.Append('"')
                    .Append(i[..10])
                    .Append("\",");
            }

            // Remove last ,
            str.Length--;
            str.Append(']');

            using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/metadata/programs");
            message.Headers.TryAddWithoutValidation("token", token);
            message.Content = new StringContent(str.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            try
            {
                return await Request<IReadOnlyList<ShowImagesDto>>(message, true, info, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image info from schedules direct");

                return [];
            }
        }

        public async Task<List<NameIdPair>> GetHeadends(ListingsProviderInfo info, string country, string location, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            var lineups = new List<NameIdPair>();

            if (string.IsNullOrWhiteSpace(token))
            {
                return lineups;
            }

            using var options = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/headends?country=" + country + "&postalcode=" + location);
            options.Headers.TryAddWithoutValidation("token", token);

            try
            {
                var root = await Request<IReadOnlyList<HeadendsDto>>(options, false, info, cancellationToken).ConfigureAwait(false);
                if (root is not null)
                {
                    foreach (HeadendsDto headend in root)
                    {
                        foreach (LineupDto lineup in headend.Lineups)
                        {
                            lineups.Add(new NameIdPair
                            {
                                Name = string.IsNullOrWhiteSpace(lineup.Name) ? lineup.Lineup : lineup.Name,
                                Id = lineup.Uri?[18..]
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No lineups available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting headends");
            }

            return lineups;
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

            // Avoid hammering SD
            if ((DateTime.UtcNow - _lastErrorResponse).TotalMinutes < 1)
            {
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
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.BadRequest)
                    {
                        _tokens.Clear();
                        _lastErrorResponse = DateTime.UtcNow;
                    }

                    throw;
                }
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

            if (!enableRetry || (int)response.StatusCode >= 500)
            {
                _logger.LogError(
                    "Request to {Url} failed with response {Response}",
                    message.RequestUri,
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

                throw new HttpRequestException(
                    string.Format(CultureInfo.InvariantCulture, "Request failed: {0}", response.ReasonPhrase),
                    null,
                    response.StatusCode);
            }

            _tokens.Clear();
            using var retryMessage = new HttpRequestMessage(message.Method, message.RequestUri);
            retryMessage.Content = message.Content;
            retryMessage.Headers.TryAddWithoutValidation(
                "token",
                await GetToken(providerInfo, cancellationToken).ConfigureAwait(false));

            return await Request<T>(retryMessage, false, providerInfo, cancellationToken).ConfigureAwait(false);
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
            // TODO: remove ToLower when Convert.ToHexString supports lowercase
            // Schedules Direct requires the hex to be lowercase
            string hashedPassword = Convert.ToHexString(hashedPasswordBytes).ToLowerInvariant();
            options.Content = new StringContent("{\"username\":\"" + username + "\",\"password\":\"" + hashedPassword + "\"}", Encoding.UTF8, MediaTypeNames.Application.Json);

            var root = await Request<TokenDto>(options, false, null, cancellationToken).ConfigureAwait(false);
            if (string.Equals(root?.Message, "OK", StringComparison.Ordinal))
            {
                _logger.LogInformation("Authenticated with Schedules Direct token: {Token}", root.Token);
                return root.Token;
            }

            throw new AuthenticationException("Could not authenticate with Schedules Direct Error: " + root.Message);
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
                _logger.LogError(
                    "Error adding lineup to account: {Response}",
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
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

        public async Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            if (validateLogin)
            {
                ArgumentException.ThrowIfNullOrEmpty(info.Username);
                ArgumentException.ThrowIfNullOrEmpty(info.Password);
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
            ArgumentException.ThrowIfNullOrEmpty(listingsId);

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            ArgumentException.ThrowIfNullOrEmpty(token);

            using var options = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "/lineups/" + listingsId);
            options.Headers.TryAddWithoutValidation("token", token);

            var root = await Request<ChannelDto>(options, true, info, cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return new List<ChannelInfo>();
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
            }

            _disposed = true;
        }
    }
}
