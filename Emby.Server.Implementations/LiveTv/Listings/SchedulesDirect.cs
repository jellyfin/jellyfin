#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.Listings
{
    public class SchedulesDirect : IListingsProvider
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly IApplicationHost _appHost;

        private const string ApiUrl = "https://json.schedulesdirect.org/20141201";

        public SchedulesDirect(
            ILogger<SchedulesDirect> logger,
            IJsonSerializer jsonSerializer,
            IHttpClient httpClient,
            IApplicationHost appHost)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _appHost = appHost;
        }

        private string UserAgent => _appHost.ApplicationUserAgent;

        /// <inheritdoc />
        public string Name => "Schedules Direct";

        /// <inheritdoc />
        public string Type => nameof(SchedulesDirect);

        private static List<string> GetScheduleRequestDates(DateTime startDateUtc, DateTime endDateUtc)
        {
            var dates = new List<string>();

            var start = new List<DateTime> { startDateUtc, startDateUtc.ToLocalTime() }.Min().Date;
            var end = new List<DateTime> { endDateUtc, endDateUtc.ToLocalTime() }.Max().Date;

            while (start <= end)
            {
                dates.Add(start.ToString("yyyy-MM-dd"));
                start = start.AddDays(1);
            }

            return dates;
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            // Normalize incoming input
            channelId = channelId.Replace(".json.schedulesdirect.org", string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('I');

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("SchedulesDirect token is empty, returning empty program list");

                return Enumerable.Empty<ProgramInfo>();
            }

            var dates = GetScheduleRequestDates(startDateUtc, endDateUtc);

            _logger.LogInformation("Channel Station ID is: {ChannelID}", channelId);
            var requestList = new List<ScheduleDirect.RequestScheduleForChannel>()
                {
                    new ScheduleDirect.RequestScheduleForChannel()
                    {
                        stationID = channelId,
                        date = dates
                    }
                };

            var requestString = _jsonSerializer.SerializeToString(requestList);
            _logger.LogDebug("Request string for schedules is: {RequestString}", requestString);

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/schedules",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
                RequestContent = requestString
            };

            httpOptions.RequestHeaders["token"] = token;

            using (var response = await Post(httpOptions, true, info).ConfigureAwait(false))
            {
                var dailySchedules = await _jsonSerializer.DeserializeFromStreamAsync<List<ScheduleDirect.Day>>(response.Content).ConfigureAwait(false);
                _logger.LogDebug("Found {ScheduleCount} programs on {ChannelID} ScheduleDirect", dailySchedules.Count, channelId);

                httpOptions = new HttpRequestOptions()
                {
                    Url = ApiUrl + "/programs",
                    UserAgent = UserAgent,
                    CancellationToken = cancellationToken,
                    LogErrorResponseBody = true
                };

                httpOptions.RequestHeaders["token"] = token;

                var programsID = dailySchedules.SelectMany(d => d.programs.Select(s => s.programID)).Distinct();
                httpOptions.RequestContent = "[\"" + string.Join("\", \"", programsID) + "\"]";

                using (var innerResponse = await Post(httpOptions, true, info).ConfigureAwait(false))
                {
                    var programDetails = await _jsonSerializer.DeserializeFromStreamAsync<List<ScheduleDirect.ProgramDetails>>(innerResponse.Content).ConfigureAwait(false);
                    var programDict = programDetails.ToDictionary(p => p.programID, y => y);

                    var programIdsWithImages =
                        programDetails.Where(p => p.hasImageArtwork).Select(p => p.programID)
                        .ToList();

                    var images = await GetImageForPrograms(info, programIdsWithImages, cancellationToken).ConfigureAwait(false);

                    var programsInfo = new List<ProgramInfo>();
                    foreach (ScheduleDirect.Program schedule in dailySchedules.SelectMany(d => d.programs))
                    {
                        //_logger.LogDebug("Proccesing Schedule for statio ID " + stationID +
                        //              " which corresponds to channel " + channelNumber + " and program id " +
                        //              schedule.programID + " which says it has images? " +
                        //              programDict[schedule.programID].hasImageArtwork);

                        if (images != null)
                        {
                            var imageIndex = images.FindIndex(i => i.programID == schedule.programID.Substring(0, 10));
                            if (imageIndex > -1)
                            {
                                var programEntry = programDict[schedule.programID];

                                var allImages = images[imageIndex].data ?? new List<ScheduleDirect.ImageData>();
                                var imagesWithText = allImages.Where(i => string.Equals(i.text, "yes", StringComparison.OrdinalIgnoreCase));
                                var imagesWithoutText = allImages.Where(i => string.Equals(i.text, "no", StringComparison.OrdinalIgnoreCase));

                                const double DesiredAspect = 2.0 / 3;

                                programEntry.primaryImage = GetProgramImage(ApiUrl, imagesWithText, true, DesiredAspect) ??
                                    GetProgramImage(ApiUrl, allImages, true, DesiredAspect);

                                const double WideAspect = 16.0 / 9;

                                programEntry.thumbImage = GetProgramImage(ApiUrl, imagesWithText, true, WideAspect);

                                // Don't supply the same image twice
                                if (string.Equals(programEntry.primaryImage, programEntry.thumbImage, StringComparison.Ordinal))
                                {
                                    programEntry.thumbImage = null;
                                }

                                programEntry.backdropImage = GetProgramImage(ApiUrl, imagesWithoutText, true, WideAspect);

                                //programEntry.bannerImage = GetProgramImage(ApiUrl, data, "Banner", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-L1", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-LO", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-LOT", false);
                            }
                        }

                        programsInfo.Add(GetProgram(channelId, schedule, programDict[schedule.programID]));
                    }

                    return programsInfo;
                }
            }
        }

        private static int GetSizeOrder(ScheduleDirect.ImageData image)
        {
            if (!string.IsNullOrWhiteSpace(image.height)
                && int.TryParse(image.height, out int value))
            {
                return value;
            }

            return 0;
        }

        private static string GetChannelNumber(ScheduleDirect.Map map)
        {
            var channelNumber = map.logicalChannelNumber;

            if (string.IsNullOrWhiteSpace(channelNumber))
            {
                channelNumber = map.channel;
            }
            if (string.IsNullOrWhiteSpace(channelNumber))
            {
                channelNumber = map.atscMajor + "." + map.atscMinor;
            }

            return channelNumber.TrimStart('0');
        }

        private static bool IsMovie(ScheduleDirect.ProgramDetails programInfo)
        {
            return string.Equals(programInfo.entityType, "movie", StringComparison.OrdinalIgnoreCase);
        }

        private ProgramInfo GetProgram(string channelId, ScheduleDirect.Program programInfo, ScheduleDirect.ProgramDetails details)
        {
            var startAt = GetDate(programInfo.airDateTime);
            var endAt = startAt.AddSeconds(programInfo.duration);
            var audioType = ProgramAudio.Stereo;

            var programId = programInfo.programID ?? string.Empty;

            string newID = programId + "T" + startAt.Ticks + "C" + channelId;

            if (programInfo.audioProperties != null)
            {
                if (programInfo.audioProperties.Exists(item => string.Equals(item, "atmos", StringComparison.OrdinalIgnoreCase)))
                {
                    audioType = ProgramAudio.Atmos;
                }
                else if (programInfo.audioProperties.Exists(item => string.Equals(item, "dd 5.1", StringComparison.OrdinalIgnoreCase)))
                {
                    audioType = ProgramAudio.DolbyDigital;
                }
                else if (programInfo.audioProperties.Exists(item => string.Equals(item, "dd", StringComparison.OrdinalIgnoreCase)))
                {
                    audioType = ProgramAudio.DolbyDigital;
                }
                else if (programInfo.audioProperties.Exists(item => string.Equals(item, "stereo", StringComparison.OrdinalIgnoreCase)))
                {
                    audioType = ProgramAudio.Stereo;
                }
                else
                {
                    audioType = ProgramAudio.Mono;
                }
            }

            string episodeTitle = null;
            if (details.episodeTitle150 != null)
            {
                episodeTitle = details.episodeTitle150;
            }

            var info = new ProgramInfo
            {
                ChannelId = channelId,
                Id = newID,
                StartDate = startAt,
                EndDate = endAt,
                Name = details.titles[0].title120 ?? "Unkown",
                OfficialRating = null,
                CommunityRating = null,
                EpisodeTitle = episodeTitle,
                Audio = audioType,
                //IsNew = programInfo.@new ?? false,
                IsRepeat = programInfo.@new == null,
                IsSeries = string.Equals(details.entityType, "episode", StringComparison.OrdinalIgnoreCase),
                ImageUrl = details.primaryImage,
                ThumbImageUrl = details.thumbImage,
                IsKids = string.Equals(details.audience, "children", StringComparison.OrdinalIgnoreCase),
                IsSports = string.Equals(details.entityType, "sports", StringComparison.OrdinalIgnoreCase),
                IsMovie = IsMovie(details),
                Etag = programInfo.md5,
                IsLive = string.Equals(programInfo.liveTapeDelay, "live", StringComparison.OrdinalIgnoreCase),
                IsPremiere = programInfo.premiere || (programInfo.isPremiereOrFinale ?? string.Empty).IndexOf("premiere", StringComparison.OrdinalIgnoreCase) != -1
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

            if (programInfo.videoProperties != null)
            {
                info.IsHD = programInfo.videoProperties.Contains("hdtv", StringComparer.OrdinalIgnoreCase);
                info.Is3D = programInfo.videoProperties.Contains("3d", StringComparer.OrdinalIgnoreCase);
            }

            if (details.contentRating != null && details.contentRating.Count > 0)
            {
                info.OfficialRating = details.contentRating[0].code.Replace("TV", "TV-").Replace("--", "-");

                var invalid = new[] { "N/A", "Approved", "Not Rated", "Passed" };
                if (invalid.Contains(info.OfficialRating, StringComparer.OrdinalIgnoreCase))
                {
                    info.OfficialRating = null;
                }
            }

            if (details.descriptions != null)
            {
                if (details.descriptions.description1000 != null && details.descriptions.description1000.Count > 0)
                {
                    info.Overview = details.descriptions.description1000[0].description;
                }
                else if (details.descriptions.description100 != null && details.descriptions.description100.Count > 0)
                {
                    info.Overview = details.descriptions.description100[0].description;
                }
            }

            if (info.IsSeries)
            {
                info.SeriesId = programId.Substring(0, 10);

                info.SeriesProviderIds[MetadataProviders.Zap2It.ToString()] = info.SeriesId;

                if (details.metadata != null)
                {
                    foreach (var metadataProgram in details.metadata)
                    {
                        var gracenote = metadataProgram.Gracenote;
                        if (gracenote != null)
                        {
                            info.SeasonNumber = gracenote.season;

                            if (gracenote.episode > 0)
                            {
                                info.EpisodeNumber = gracenote.episode;
                            }

                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(details.originalAirDate))
            {
                info.OriginalAirDate = DateTime.Parse(details.originalAirDate);
                info.ProductionYear = info.OriginalAirDate.Value.Year;
            }

            if (details.movie != null)
            {
                if (!string.IsNullOrEmpty(details.movie.year) && int.TryParse(details.movie.year, out int year))
                {
                    info.ProductionYear = year;
                }
            }

            if (details.genres != null)
            {
                info.Genres = details.genres.Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                info.IsNews = details.genres.Contains("news", StringComparer.OrdinalIgnoreCase);

                if (info.Genres.Contains("children", StringComparer.OrdinalIgnoreCase))
                {
                    info.IsKids = true;
                }
            }

            return info;
        }

        private static DateTime GetDate(string value)
        {
            var date = DateTime.ParseExact(value, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);

            if (date.Kind != DateTimeKind.Utc)
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
            return date;
        }

        private string GetProgramImage(string apiUrl, IEnumerable<ScheduleDirect.ImageData> images, bool returnDefaultImage, double desiredAspect)
        {
            var match = images
                .OrderBy(i => Math.Abs(desiredAspect - GetAspectRatio(i)))
                .ThenByDescending(GetSizeOrder)
                .FirstOrDefault();

            if (match == null)
            {
                return null;
            }

            var uri = match.uri;

            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }
            else if (uri.IndexOf("http", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return uri;
            }
            else
            {
                return apiUrl + "/image/" + uri;
            }
        }

        private static double GetAspectRatio(ScheduleDirect.ImageData i)
        {
            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(i.width))
            {
                int.TryParse(i.width, out width);
            }

            if (!string.IsNullOrWhiteSpace(i.height))
            {
                int.TryParse(i.height, out height);
            }

            if (height == 0 || width == 0)
            {
                return 0;
            }

            double result = width;
            result /= height;
            return result;
        }

        private async Task<List<ScheduleDirect.ShowImages>> GetImageForPrograms(
            ListingsProviderInfo info,
            List<string> programIds,
           CancellationToken cancellationToken)
        {
            if (programIds.Count == 0)
            {
                return new List<ScheduleDirect.ShowImages>();
            }

            var imageIdString = "[";

            foreach (var i in programIds)
            {
                var imageId = i.Substring(0, 10);

                if (!imageIdString.Contains(imageId))
                {
                    imageIdString += "\"" + imageId + "\",";
                }
            }

            imageIdString = imageIdString.TrimEnd(',') + "]";

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/metadata/programs",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                RequestContent = imageIdString,
                LogErrorResponseBody = true,
            };

            try
            {
                using (var innerResponse2 = await Post(httpOptions, true, info).ConfigureAwait(false))
                {
                    return await _jsonSerializer.DeserializeFromStreamAsync<List<ScheduleDirect.ShowImages>>(
                        innerResponse2.Content).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image info from schedules direct");

                return new List<ScheduleDirect.ShowImages>();
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

            var options = new HttpRequestOptions()
            {
                Url = ApiUrl + "/headends?country=" + country + "&postalcode=" + location,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true
            };

            options.RequestHeaders["token"] = token;

            try
            {
                using (var httpResponse = await Get(options, false, info).ConfigureAwait(false))
                using (Stream responce = httpResponse.Content)
                {
                    var root = await _jsonSerializer.DeserializeFromStreamAsync<List<ScheduleDirect.Headends>>(responce).ConfigureAwait(false);

                    if (root != null)
                    {
                        foreach (ScheduleDirect.Headends headend in root)
                        {
                            foreach (ScheduleDirect.Lineup lineup in headend.lineups)
                            {
                                lineups.Add(new NameIdPair
                                {
                                    Name = string.IsNullOrWhiteSpace(lineup.name) ? lineup.lineup : lineup.name,
                                    Id = lineup.uri.Substring(18)
                                });
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No lineups available");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting headends");
            }

            return lineups;
        }

        private readonly ConcurrentDictionary<string, NameValuePair> _tokens = new ConcurrentDictionary<string, NameValuePair>();
        private DateTime _lastErrorResponse;
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

            NameValuePair savedToken = null;
            if (!_tokens.TryGetValue(username, out savedToken))
            {
                savedToken = new NameValuePair();
                _tokens.TryAdd(username, savedToken);
            }

            if (!string.IsNullOrEmpty(savedToken.Name) && !string.IsNullOrEmpty(savedToken.Value))
            {
                if (long.TryParse(savedToken.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out long ticks))
                {
                    // If it's under 24 hours old we can still use it
                    if (DateTime.UtcNow.Ticks - ticks < TimeSpan.FromHours(20).Ticks)
                    {
                        return savedToken.Name;
                    }
                }
            }

            await _tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await GetTokenInternal(username, password, cancellationToken).ConfigureAwait(false);
                savedToken.Name = result;
                savedToken.Value = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
                return result;
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode.HasValue)
                {
                    if ((int)ex.StatusCode.Value == 400)
                    {
                        _tokens.Clear();
                        _lastErrorResponse = DateTime.UtcNow;
                    }
                }
                throw;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        private async Task<HttpResponseInfo> Post(HttpRequestOptions options,
            bool enableRetry,
            ListingsProviderInfo providerInfo)
        {
            // Schedules direct requires that the client support compression and will return a 400 response without it
            options.DecompressionMethod = CompressionMethods.Deflate;

            try
            {
                return await _httpClient.Post(options).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                _tokens.Clear();

                if (!ex.StatusCode.HasValue || (int)ex.StatusCode.Value >= 500)
                {
                    enableRetry = false;
                }

                if (!enableRetry)
                {
                    throw;
                }
            }

            options.RequestHeaders["token"] = await GetToken(providerInfo, options.CancellationToken).ConfigureAwait(false);
            return await Post(options, false, providerInfo).ConfigureAwait(false);
        }

        private async Task<HttpResponseInfo> Get(HttpRequestOptions options,
            bool enableRetry,
            ListingsProviderInfo providerInfo)
        {
            // Schedules direct requires that the client support compression and will return a 400 response without it
            options.DecompressionMethod = CompressionMethods.Deflate;

            try
            {
                return await _httpClient.SendAsync(options, HttpMethod.Get).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                _tokens.Clear();

                if (!ex.StatusCode.HasValue || (int)ex.StatusCode.Value >= 500)
                {
                    enableRetry = false;
                }

                if (!enableRetry)
                {
                    throw;
                }
            }

            options.RequestHeaders["token"] = await GetToken(providerInfo, options.CancellationToken).ConfigureAwait(false);
            return await Get(options, false, providerInfo).ConfigureAwait(false);
        }

        private async Task<string> GetTokenInternal(string username, string password,
            CancellationToken cancellationToken)
        {
            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/token",
                UserAgent = UserAgent,
                RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true
            };
            //_logger.LogInformation("Obtaining token from Schedules Direct from addres: " + httpOptions.Url + " with body " +
            // httpOptions.RequestContent);

            using (var response = await Post(httpOptions, false, null).ConfigureAwait(false))
            {
                var root = await _jsonSerializer.DeserializeFromStreamAsync<ScheduleDirect.Token>(response.Content).ConfigureAwait(false);
                if (root.message == "OK")
                {
                    _logger.LogInformation("Authenticated with Schedules Direct token: " + root.token);
                    return root.token;
                }

                throw new Exception("Could not authenticate with Schedules Direct Error: " + root.message);
            }
        }

        private async Task AddLineupToAccount(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Authentication required.");
            }

            if (string.IsNullOrEmpty(info.ListingsId))
            {
                throw new ArgumentException("Listings Id required");
            }

            _logger.LogInformation("Adding new LineUp ");

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups/" + info.ListingsId,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
                BufferContent = false
            };

            httpOptions.RequestHeaders["token"] = token;

            using (await _httpClient.SendAsync(httpOptions, HttpMethod.Put).ConfigureAwait(false))
            {
            }
        }

        private async Task<bool> HasLineup(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(info.ListingsId))
            {
                throw new ArgumentException("Listings Id required");
            }

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("token required");
            }

            _logger.LogInformation("Headends on account ");

            var options = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true
            };

            options.RequestHeaders["token"] = token;

            try
            {
                using (var httpResponse = await Get(options, false, null).ConfigureAwait(false))
                using (var response = httpResponse.Content)
                {
                    var root = await _jsonSerializer.DeserializeFromStreamAsync<ScheduleDirect.Lineups>(response).ConfigureAwait(false);

                    return root.lineups.Any(i => string.Equals(info.ListingsId, i.lineup, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (HttpException ex)
            {
                // Apparently we're supposed to swallow this
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.BadRequest)
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
                if (string.IsNullOrEmpty(info.Username))
                {
                    throw new ArgumentException("Username is required");
                }
                if (string.IsNullOrEmpty(info.Password))
                {
                    throw new ArgumentException("Password is required");
                }
            }
            if (validateListings)
            {
                if (string.IsNullOrEmpty(info.ListingsId))
                {
                    throw new ArgumentException("Listings Id required");
                }

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
                throw new Exception("ListingsId required");
            }

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("token required");
            }

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups/" + listingsId,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
            };

            httpOptions.RequestHeaders["token"] = token;

            var list = new List<ChannelInfo>();

            using (var httpResponse = await Get(httpOptions, true, info).ConfigureAwait(false))
            using (var response = httpResponse.Content)
            {
                var root = await _jsonSerializer.DeserializeFromStreamAsync<ScheduleDirect.Channel>(response).ConfigureAwait(false);
                _logger.LogInformation("Found {ChannelCount} channels on the lineup on ScheduleDirect", root.map.Count);
                _logger.LogInformation("Mapping Stations to Channel");

                var allStations = root.stations ?? Enumerable.Empty<ScheduleDirect.Station>();

                foreach (ScheduleDirect.Map map in root.map)
                {
                    var channelNumber = GetChannelNumber(map);

                    var station = allStations.FirstOrDefault(item => string.Equals(item.stationID, map.stationID, StringComparison.OrdinalIgnoreCase));
                    if (station == null)
                    {
                        station = new ScheduleDirect.Station
                        {
                            stationID = map.stationID
                        };
                    }

                    var channelInfo = new ChannelInfo
                    {
                        Id = station.stationID,
                        CallSign = station.callsign,
                        Number = channelNumber,
                        Name = string.IsNullOrWhiteSpace(station.name) ? channelNumber : station.name
                    };

                    if (station.logo != null)
                    {
                        channelInfo.ImageUrl = station.logo.URL;
                    }

                    list.Add(channelInfo);
                }
            }

            return list;
        }

        private ScheduleDirect.Station GetStation(List<ScheduleDirect.Station> allStations, string channelNumber, string channelName)
        {
            if (!string.IsNullOrWhiteSpace(channelName))
            {
                channelName = NormalizeName(channelName);

                var result = allStations.FirstOrDefault(i => string.Equals(NormalizeName(i.callsign ?? string.Empty), channelName, StringComparison.OrdinalIgnoreCase));

                if (result != null)
                {
                    return result;
                }
            }

            if (!string.IsNullOrWhiteSpace(channelNumber))
            {
                return allStations.FirstOrDefault(i => string.Equals(NormalizeName(i.stationID ?? string.Empty), channelNumber, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static string NormalizeName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        public class ScheduleDirect
        {
            public class Token
            {
                public int code { get; set; }
                public string message { get; set; }
                public string serverID { get; set; }
                public string token { get; set; }
            }
            public class Lineup
            {
                public string lineup { get; set; }
                public string name { get; set; }
                public string transport { get; set; }
                public string location { get; set; }
                public string uri { get; set; }
            }

            public class Lineups
            {
                public int code { get; set; }
                public string serverID { get; set; }
                public string datetime { get; set; }
                public List<Lineup> lineups { get; set; }
            }


            public class Headends
            {
                public string headend { get; set; }
                public string transport { get; set; }
                public string location { get; set; }
                public List<Lineup> lineups { get; set; }
            }



            public class Map
            {
                public string stationID { get; set; }
                public string channel { get; set; }
                public string logicalChannelNumber { get; set; }
                public int uhfVhf { get; set; }
                public int atscMajor { get; set; }
                public int atscMinor { get; set; }
            }

            public class Broadcaster
            {
                public string city { get; set; }
                public string state { get; set; }
                public string postalcode { get; set; }
                public string country { get; set; }
            }

            public class Logo
            {
                public string URL { get; set; }
                public int height { get; set; }
                public int width { get; set; }
                public string md5 { get; set; }
            }

            public class Station
            {
                public string stationID { get; set; }
                public string name { get; set; }
                public string callsign { get; set; }
                public List<string> broadcastLanguage { get; set; }
                public List<string> descriptionLanguage { get; set; }
                public Broadcaster broadcaster { get; set; }
                public string affiliate { get; set; }
                public Logo logo { get; set; }
                public bool? isCommercialFree { get; set; }
            }

            public class Metadata
            {
                public string lineup { get; set; }
                public string modified { get; set; }
                public string transport { get; set; }
            }

            public class Channel
            {
                public List<Map> map { get; set; }
                public List<Station> stations { get; set; }
                public Metadata metadata { get; set; }
            }

            public class RequestScheduleForChannel
            {
                public string stationID { get; set; }
                public List<string> date { get; set; }
            }




            public class Rating
            {
                public string body { get; set; }
                public string code { get; set; }
            }

            public class Multipart
            {
                public int partNumber { get; set; }
                public int totalParts { get; set; }
            }

            public class Program
            {
                public string programID { get; set; }
                public string airDateTime { get; set; }
                public int duration { get; set; }
                public string md5 { get; set; }
                public List<string> audioProperties { get; set; }
                public List<string> videoProperties { get; set; }
                public List<Rating> ratings { get; set; }
                public bool? @new { get; set; }
                public Multipart multipart { get; set; }
                public string liveTapeDelay { get; set; }
                public bool premiere { get; set; }
                public bool repeat { get; set; }
                public string isPremiereOrFinale { get; set; }
            }



            public class MetadataSchedule
            {
                public string modified { get; set; }
                public string md5 { get; set; }
                public string startDate { get; set; }
                public string endDate { get; set; }
                public int days { get; set; }
            }

            public class Day
            {
                public string stationID { get; set; }
                public List<Program> programs { get; set; }
                public MetadataSchedule metadata { get; set; }

                public Day()
                {
                    programs = new List<Program>();
                }
            }

            //
            public class Title
            {
                public string title120 { get; set; }
            }

            public class EventDetails
            {
                public string subType { get; set; }
            }

            public class Description100
            {
                public string descriptionLanguage { get; set; }
                public string description { get; set; }
            }

            public class Description1000
            {
                public string descriptionLanguage { get; set; }
                public string description { get; set; }
            }

            public class DescriptionsProgram
            {
                public List<Description100> description100 { get; set; }
                public List<Description1000> description1000 { get; set; }
            }

            public class Gracenote
            {
                public int season { get; set; }
                public int episode { get; set; }
            }

            public class MetadataPrograms
            {
                public Gracenote Gracenote { get; set; }
            }

            public class ContentRating
            {
                public string body { get; set; }
                public string code { get; set; }
            }

            public class Cast
            {
                public string billingOrder { get; set; }
                public string role { get; set; }
                public string nameId { get; set; }
                public string personId { get; set; }
                public string name { get; set; }
                public string characterName { get; set; }
            }

            public class Crew
            {
                public string billingOrder { get; set; }
                public string role { get; set; }
                public string nameId { get; set; }
                public string personId { get; set; }
                public string name { get; set; }
            }

            public class QualityRating
            {
                public string ratingsBody { get; set; }
                public string rating { get; set; }
                public string minRating { get; set; }
                public string maxRating { get; set; }
                public string increment { get; set; }
            }

            public class Movie
            {
                public string year { get; set; }
                public int duration { get; set; }
                public List<QualityRating> qualityRating { get; set; }
            }

            public class Recommendation
            {
                public string programID { get; set; }
                public string title120 { get; set; }
            }

            public class ProgramDetails
            {
                public string audience { get; set; }
                public string programID { get; set; }
                public List<Title> titles { get; set; }
                public EventDetails eventDetails { get; set; }
                public DescriptionsProgram descriptions { get; set; }
                public string originalAirDate { get; set; }
                public List<string> genres { get; set; }
                public string episodeTitle150 { get; set; }
                public List<MetadataPrograms> metadata { get; set; }
                public List<ContentRating> contentRating { get; set; }
                public List<Cast> cast { get; set; }
                public List<Crew> crew { get; set; }
                public string entityType { get; set; }
                public string showType { get; set; }
                public bool hasImageArtwork { get; set; }
                public string primaryImage { get; set; }
                public string thumbImage { get; set; }
                public string backdropImage { get; set; }
                public string bannerImage { get; set; }
                public string imageID { get; set; }
                public string md5 { get; set; }
                public List<string> contentAdvisory { get; set; }
                public Movie movie { get; set; }
                public List<Recommendation> recommendations { get; set; }
            }

            public class Caption
            {
                public string content { get; set; }
                public string lang { get; set; }
            }

            public class ImageData
            {
                public string width { get; set; }
                public string height { get; set; }
                public string uri { get; set; }
                public string size { get; set; }
                public string aspect { get; set; }
                public string category { get; set; }
                public string text { get; set; }
                public string primary { get; set; }
                public string tier { get; set; }
                public Caption caption { get; set; }
            }

            public class ShowImages
            {
                public string programID { get; set; }
                public List<ImageData> data { get; set; }
            }

        }
    }
}
