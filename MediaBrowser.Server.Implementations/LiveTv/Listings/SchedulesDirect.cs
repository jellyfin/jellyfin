using System.Net;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings
{
    public class SchedulesDirect : IListingsProvider
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly IApplicationHost _appHost;

        private const string ApiUrl = "https://json.schedulesdirect.org/20141201";

        private readonly Dictionary<string, Dictionary<string, ScheduleDirect.Station>> _channelPairingCache =
            new Dictionary<string, Dictionary<string, ScheduleDirect.Station>>(StringComparer.OrdinalIgnoreCase);

        public SchedulesDirect(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient, IApplicationHost appHost)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _appHost = appHost;
        }

        private string UserAgent
        {
            get { return "Emby/" + _appHost.ApplicationVersion; }
        }

        private List<string> GetScheduleRequestDates(DateTime startDateUtc, DateTime endDateUtc)
        {
            List<string> dates = new List<string>();

            var start = new List<DateTime> { startDateUtc, startDateUtc.ToLocalTime() }.Min().Date;
            var end = new List<DateTime> { endDateUtc, endDateUtc.ToLocalTime() }.Max().Date;

            while (start <= end)
            {
                dates.Add(start.ToString("yyyy-MM-dd"));
                start = start.AddDays(1);
            }

            return dates;
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, string channelName, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            List<ProgramInfo> programsInfo = new List<ProgramInfo>();

            var token = await GetToken(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.Warn("SchedulesDirect token is empty, returning empty program list");
                return programsInfo;
            }

            if (string.IsNullOrWhiteSpace(info.ListingsId))
            {
                _logger.Warn("ListingsId is null, returning empty program list");
                return programsInfo;
            }

            var dates = GetScheduleRequestDates(startDateUtc, endDateUtc);

            ScheduleDirect.Station station = GetStation(info.ListingsId, channelNumber, channelName);

            if (station == null)
            {
                _logger.Info("No Schedules Direct Station found for channel {0} with name {1}", channelNumber, channelName);
                return programsInfo;
            }

            string stationID = station.stationID;

            _logger.Info("Channel Station ID is: " + stationID);
            List<ScheduleDirect.RequestScheduleForChannel> requestList =
                new List<ScheduleDirect.RequestScheduleForChannel>()
                    {
                        new ScheduleDirect.RequestScheduleForChannel()
                        {
                            stationID = stationID,
                            date = dates
                        }
                    };

            var requestString = _jsonSerializer.SerializeToString(requestList);
            _logger.Debug("Request string for schedules is: " + requestString);

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/schedules",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                // The data can be large so give it some extra time
                TimeoutMs = 60000,
                LogErrorResponseBody = true
            };

            httpOptions.RequestHeaders["token"] = token;

            httpOptions.RequestContent = requestString;
            using (var response = await Post(httpOptions, true, info).ConfigureAwait(false))
            {
                StreamReader reader = new StreamReader(response.Content);
                string responseString = reader.ReadToEnd();
                var dailySchedules = _jsonSerializer.DeserializeFromString<List<ScheduleDirect.Day>>(responseString);
                _logger.Debug("Found " + dailySchedules.Count + " programs on " + channelNumber + " ScheduleDirect");

                httpOptions = new HttpRequestOptions()
                {
                    Url = ApiUrl + "/programs",
                    UserAgent = UserAgent,
                    CancellationToken = cancellationToken,
                    LogErrorResponseBody = true,
                    // The data can be large so give it some extra time
                    TimeoutMs = 60000
                };

                httpOptions.RequestHeaders["token"] = token;

                List<string> programsID = new List<string>();
                programsID = dailySchedules.SelectMany(d => d.programs.Select(s => s.programID)).Distinct().ToList();
                var requestBody = "[\"" + string.Join("\", \"", programsID) + "\"]";
                httpOptions.RequestContent = requestBody;

                using (var innerResponse = await Post(httpOptions, true, info).ConfigureAwait(false))
                {
                    StreamReader innerReader = new StreamReader(innerResponse.Content);
                    responseString = innerReader.ReadToEnd();

                    var programDetails =
                        _jsonSerializer.DeserializeFromString<List<ScheduleDirect.ProgramDetails>>(
                            responseString);
                    var programDict = programDetails.ToDictionary(p => p.programID, y => y);

                    var images = await GetImageForPrograms(info, programDetails.Where(p => p.hasImageArtwork).Select(p => p.programID).ToList(), cancellationToken);

                    var schedules = dailySchedules.SelectMany(d => d.programs);
                    foreach (ScheduleDirect.Program schedule in schedules)
                    {
                        //_logger.Debug("Proccesing Schedule for statio ID " + stationID +
                        //              " which corresponds to channel " + channelNumber + " and program id " +
                        //              schedule.programID + " which says it has images? " +
                        //              programDict[schedule.programID].hasImageArtwork);

                        if (images != null)
                        {
                            var imageIndex = images.FindIndex(i => i.programID == schedule.programID.Substring(0, 10));
                            if (imageIndex > -1)
                            {
                                var programEntry = programDict[schedule.programID];

                                var data = images[imageIndex].data ?? new List<ScheduleDirect.ImageData>();
                                data = data.OrderByDescending(GetSizeOrder).ToList();

                                programEntry.primaryImage = GetProgramImage(ApiUrl, data, "Logo", true, 600);
                                //programEntry.thumbImage = GetProgramImage(ApiUrl, data, "Iconic", false);
                                //programEntry.bannerImage = GetProgramImage(ApiUrl, data, "Banner", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-L1", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-LO", false) ??
                                //    GetProgramImage(ApiUrl, data, "Banner-LOT", false);
                            }
                        }

                        programsInfo.Add(GetProgram(channelNumber, schedule, programDict[schedule.programID]));
                    }
                    _logger.Info("Finished with EPGData");
                }
            }

            return programsInfo;
        }

        private int GetSizeOrder(ScheduleDirect.ImageData image)
        {
            if (!string.IsNullOrWhiteSpace(image.height))
            {
                int value;
                if (int.TryParse(image.height, out value))
                {
                    return value;
                }
            }

            return 0;
        }

        private readonly object _channelCacheLock = new object();
        private ScheduleDirect.Station GetStation(string listingsId, string channelNumber, string channelName)
        {
            lock (_channelCacheLock)
            {
                Dictionary<string, ScheduleDirect.Station> channelPair;
                if (_channelPairingCache.TryGetValue(listingsId, out channelPair))
                {
                    ScheduleDirect.Station station;

                    if (channelPair.TryGetValue(channelNumber, out station))
                    {
                        return station;
                    }

                    if (!string.IsNullOrWhiteSpace(channelName))
                    {
                        channelName = NormalizeName(channelName);

                        var result = channelPair.Values.FirstOrDefault(i => string.Equals(NormalizeName(i.callsign ?? string.Empty), channelName, StringComparison.OrdinalIgnoreCase));

                        if (result != null)
                        {
                            return result;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(channelNumber))
                    {
                        return channelPair.Values.FirstOrDefault(i => string.Equals(NormalizeName(i.stationID ?? string.Empty), channelNumber, StringComparison.OrdinalIgnoreCase));
                    }
                }

                return null;
            }
        }

        private void AddToChannelPairCache(string listingsId, string channelNumber, ScheduleDirect.Station schChannel)
        {
            lock (_channelCacheLock)
            {
                Dictionary<string, ScheduleDirect.Station> cache;
                if (_channelPairingCache.TryGetValue(listingsId, out cache))
                {
                    cache[channelNumber] = schChannel;
                }
                else
                {
                    cache = new Dictionary<string, ScheduleDirect.Station>();
                    cache[channelNumber] = schChannel;
                    _channelPairingCache[listingsId] = cache;
                }
            }
        }

        private void ClearPairCache(string listingsId)
        {
            lock (_channelCacheLock)
            {
                Dictionary<string, ScheduleDirect.Station> cache;
                if (_channelPairingCache.TryGetValue(listingsId, out cache))
                {
                    cache.Clear();
                }
            }
        }

        private int GetChannelPairCacheCount(string listingsId)
        {
            lock (_channelCacheLock)
            {
                Dictionary<string, ScheduleDirect.Station> cache;
                if (_channelPairingCache.TryGetValue(listingsId, out cache))
                {
                    return cache.Count;
                }

                return 0;
            }
        }

        private string NormalizeName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        public async Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels,
            CancellationToken cancellationToken)
        {
            var listingsId = info.ListingsId;
            if (string.IsNullOrWhiteSpace(listingsId))
            {
                throw new Exception("ListingsId required");
            }

            var token = await GetToken(info, cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("token required");
            }

            ClearPairCache(listingsId);

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups/" + listingsId,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
                // The data can be large so give it some extra time
                TimeoutMs = 60000
            };

            httpOptions.RequestHeaders["token"] = token;

            using (var response = await Get(httpOptions, true, info).ConfigureAwait(false))
            {
                var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                _logger.Info("Found " + root.map.Count + " channels on the lineup on ScheduleDirect");
                _logger.Info("Mapping Stations to Channel");
                foreach (ScheduleDirect.Map map in root.map)
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
                    channelNumber = channelNumber.TrimStart('0');

                    _logger.Debug("Found channel: " + channelNumber + " in Schedules Direct");

                    var schChannel = (root.stations ?? new List<ScheduleDirect.Station>()).FirstOrDefault(item => string.Equals(item.stationID, map.stationID, StringComparison.OrdinalIgnoreCase));
                    if (schChannel != null)
                    {
                        AddToChannelPairCache(listingsId, channelNumber, schChannel);
                    }
                    else
                    {
                        AddToChannelPairCache(listingsId, channelNumber, new ScheduleDirect.Station
                        {
                            stationID = map.stationID
                        });
                    }
                }
                _logger.Info("Added " + GetChannelPairCacheCount(listingsId) + " channels to the dictionary");

                foreach (ChannelInfo channel in channels)
                {
                    var station = GetStation(listingsId, channel.Number, channel.Name);

                    if (station != null)
                    {
                        if (station.logo != null)
                        {
                            channel.ImageUrl = station.logo.URL;
                            channel.HasImage = true;
                        }

                        if (!string.IsNullOrWhiteSpace(station.name))
                        {
                            channel.Name = station.name;
                        }
                    }
                    else
                    {
                        _logger.Info("Schedules Direct doesnt have data for channel: " + channel.Number + " " + channel.Name);
                    }
                }
            }
        }

        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo,
            ScheduleDirect.ProgramDetails details)
        {
            //_logger.Debug("Show type is: " + (details.showType ?? "No ShowType"));
            DateTime startAt = GetDate(programInfo.airDateTime);
            DateTime endAt = startAt.AddSeconds(programInfo.duration);
            ProgramAudio audioType = ProgramAudio.Stereo;

            bool repeat = programInfo.@new == null;
            string newID = programInfo.programID + "T" + startAt.Ticks + "C" + channel;

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

            var showType = details.showType ?? string.Empty;

            var info = new ProgramInfo
            {
                ChannelId = channel,
                Id = newID,
                StartDate = startAt,
                EndDate = endAt,
                Name = details.titles[0].title120 ?? "Unkown",
                OfficialRating = null,
                CommunityRating = null,
                EpisodeTitle = episodeTitle,
                Audio = audioType,
                IsRepeat = repeat,
                IsSeries = showType.IndexOf("series", StringComparison.OrdinalIgnoreCase) != -1,
                ImageUrl = details.primaryImage,
                IsKids = string.Equals(details.audience, "children", StringComparison.OrdinalIgnoreCase),
                IsSports = showType.IndexOf("sports", StringComparison.OrdinalIgnoreCase) != -1,
                IsMovie = showType.IndexOf("movie", StringComparison.OrdinalIgnoreCase) != -1 || showType.IndexOf("film", StringComparison.OrdinalIgnoreCase) != -1,
                ShowId = programInfo.programID,
                Etag = programInfo.md5
            };

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
                if (details.descriptions.description1000 != null)
                {
                    info.Overview = details.descriptions.description1000[0].description;
                }
                else if (details.descriptions.description100 != null)
                {
                    info.ShortOverview = details.descriptions.description100[0].description;
                }
            }

            if (info.IsSeries)
            {
                info.SeriesId = programInfo.programID.Substring(0, 10);

                if (details.metadata != null)
                {
                    var gracenote = details.metadata.Find(x => x.Gracenote != null).Gracenote;
                    info.SeasonNumber = gracenote.season;
                    info.EpisodeNumber = gracenote.episode;
                }
            }

            if (!string.IsNullOrWhiteSpace(details.originalAirDate) && (!info.IsSeries || info.IsRepeat))
            {
                info.OriginalAirDate = DateTime.Parse(details.originalAirDate);
                info.ProductionYear = info.OriginalAirDate.Value.Year;
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

        private DateTime GetDate(string value)
        {
            var date = DateTime.ParseExact(value, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);

            if (date.Kind != DateTimeKind.Utc)
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
            return date;
        }

        private string GetProgramImage(string apiUrl, List<ScheduleDirect.ImageData> images, string category, bool returnDefaultImage, int desiredWidth)
        {
            string url = null;

            var matches = images
                .Where(i => string.Equals(i.category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                if (!returnDefaultImage)
                {
                    return null;
                }
                matches = images;
            }

            var match = matches.FirstOrDefault(i =>
            {
                if (!string.IsNullOrWhiteSpace(i.width))
                {
                    int value;
                    if (int.TryParse(i.width, out value))
                    {
                        return value <= desiredWidth;
                    }
                }

                return false;
            });

            if (match == null)
            {
                // Get the second lowest quality image, when possible
                if (matches.Count > 1)
                {
                    match = matches[matches.Count - 2];
                }
                else
                {
                    match = matches.FirstOrDefault();
                }
            }

            if (match == null)
            {
                return null;
            }

            var uri = match.uri;

            if (!string.IsNullOrWhiteSpace(uri))
            {
                if (uri.IndexOf("http", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    url = uri;
                }
                else
                {
                    url = apiUrl + "/image/" + uri;
                }
            }
            //_logger.Debug("URL for image is : " + url);
            return url;
        }

        private async Task<List<ScheduleDirect.ShowImages>> GetImageForPrograms(
            ListingsProviderInfo info,
            List<string> programIds,
           CancellationToken cancellationToken)
        {
            var imageIdString = "[";

            programIds.ForEach(i =>
            {
                if (!imageIdString.Contains(i.Substring(0, 10)))
                {
                    imageIdString += "\"" + i.Substring(0, 10) + "\",";
                }
            });
            imageIdString = imageIdString.TrimEnd(',') + "]";

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/metadata/programs",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                RequestContent = imageIdString,
                LogErrorResponseBody = true,
                // The data can be large so give it some extra time
                TimeoutMs = 60000
            };
            List<ScheduleDirect.ShowImages> images;
            using (var innerResponse2 = await Post(httpOptions, true, info).ConfigureAwait(false))
            {
                images = _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.ShowImages>>(
                    innerResponse2.Content);
            }

            return images;
        }

        public async Task<List<NameIdPair>> GetHeadends(ListingsProviderInfo info, string country, string location, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken);

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
                using (Stream responce = await Get(options, false, info).ConfigureAwait(false))
                {
                    var root = _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.Headends>>(responce);

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
                        _logger.Info("No lineups available");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting headends", ex);
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
            if (string.IsNullOrWhiteSpace(password))
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

            if (!string.IsNullOrWhiteSpace(savedToken.Name) && !string.IsNullOrWhiteSpace(savedToken.Value))
            {
                long ticks;
                if (long.TryParse(savedToken.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out ticks))
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

            var newToken = await GetToken(providerInfo, options.CancellationToken).ConfigureAwait(false);
            options.RequestHeaders["token"] = newToken;
            return await Post(options, false, providerInfo).ConfigureAwait(false);
        }

        private async Task<Stream> Get(HttpRequestOptions options,
            bool enableRetry,
            ListingsProviderInfo providerInfo)
        {
            try
            {
                return await _httpClient.Get(options).ConfigureAwait(false);
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

            var newToken = await GetToken(providerInfo, options.CancellationToken).ConfigureAwait(false);
            options.RequestHeaders["token"] = newToken;
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
            //_logger.Info("Obtaining token from Schedules Direct from addres: " + httpOptions.Url + " with body " +
            // httpOptions.RequestContent);

            using (var responce = await Post(httpOptions, false, null).ConfigureAwait(false))
            {
                var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce.Content);
                if (root.message == "OK")
                {
                    _logger.Info("Authenticated with Schedules Direct token: " + root.token);
                    return root.token;
                }

                throw new ApplicationException("Could not authenticate with Schedules Direct Error: " + root.message);
            }
        }

        private async Task AddLineupToAccount(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            var token = await GetToken(info, cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Authentication required.");
            }

            if (string.IsNullOrWhiteSpace(info.ListingsId))
            {
                throw new ArgumentException("Listings Id required");
            }

            _logger.Info("Adding new LineUp ");

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups/" + info.ListingsId,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
                BufferContent = false
            };

            httpOptions.RequestHeaders["token"] = token;

            using (var response = await _httpClient.SendAsync(httpOptions, "PUT"))
            {
            }
        }

        public string Name
        {
            get { return "Schedules Direct"; }
        }

        public static string TypeName = "SchedulesDirect";
        public string Type
        {
            get { return TypeName; }
        }

        private async Task<bool> HasLineup(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(info.ListingsId))
            {
                throw new ArgumentException("Listings Id required");
            }

            var token = await GetToken(info, cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("token required");
            }

            _logger.Info("Headends on account ");

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
                using (var response = await Get(options, false, null).ConfigureAwait(false))
                {
                    var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Lineups>(response);

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
                if (string.IsNullOrWhiteSpace(info.Username))
                {
                    throw new ArgumentException("Username is required");
                }
                if (string.IsNullOrWhiteSpace(info.Password))
                {
                    throw new ArgumentException("Password is required");
                }
            }
            if (validateListings)
            {
                if (string.IsNullOrWhiteSpace(info.ListingsId))
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
            if (string.IsNullOrWhiteSpace(listingsId))
            {
                throw new Exception("ListingsId required");
            }

            await AddMetadata(info, new List<ChannelInfo>(), cancellationToken).ConfigureAwait(false);

            var token = await GetToken(info, cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("token required");
            }

            var httpOptions = new HttpRequestOptions()
            {
                Url = ApiUrl + "/lineups/" + listingsId,
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                LogErrorResponseBody = true,
                // The data can be large so give it some extra time
                TimeoutMs = 60000
            };

            httpOptions.RequestHeaders["token"] = token;

            var list = new List<ChannelInfo>();

            using (var response = await Get(httpOptions, true, info).ConfigureAwait(false))
            {
                var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                _logger.Info("Found " + root.map.Count + " channels on the lineup on ScheduleDirect");
                _logger.Info("Mapping Stations to Channel");
                foreach (ScheduleDirect.Map map in root.map)
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
                    channelNumber = channelNumber.TrimStart('0');

                    var name = channelNumber;
                    var station = GetStation(listingsId, channelNumber, null);

                    if (station != null && !string.IsNullOrWhiteSpace(station.name))
                    {
                        name = station.name;
                    }

                    list.Add(new ChannelInfo
                    {
                        Number = channelNumber,
                        Name = name
                    });
                }
            }

            return list;
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
                public string showType { get; set; }
                public bool hasImageArtwork { get; set; }
                public string primaryImage { get; set; }
                public string thumbImage { get; set; }
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