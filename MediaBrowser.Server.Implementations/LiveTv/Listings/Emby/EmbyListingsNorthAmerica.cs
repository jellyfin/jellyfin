using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings.Emby
{
    public class EmbyListingsNorthAmerica : IEmbyListingProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public EmbyListingsNorthAmerica(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            channelNumber = NormalizeNumber(channelNumber);

            var url = "https://data.emby.media/service/listings?id=" + info.ListingsId;

            // Normalize
            startDateUtc = startDateUtc.Date;
            endDateUtc = startDateUtc.AddDays(7);

            url += "&start=" + startDateUtc.ToString("s", CultureInfo.InvariantCulture) + "Z";
            url += "&end=" + endDateUtc.ToString("s", CultureInfo.InvariantCulture) + "Z";

            var response = await GetResponse<ListingInfo[]>(url).ConfigureAwait(false);

            return response.Where(i => IncludeInResults(i, channelNumber)).Select(GetProgramInfo).OrderBy(i => i.StartDate);
        }

        private ProgramInfo GetProgramInfo(ListingInfo info)
        {
            var showType = info.showType ?? string.Empty;

            var program = new ProgramInfo
            {
                Id = info.listingID.ToString(CultureInfo.InvariantCulture),
                Name = GetStringValue(info.showName),
                HomePageUrl = GetStringValue(info.webLink),
                Overview = info.description,
                IsHD = info.hd,
                IsLive = info.live,
                IsPremiere = info.seasonPremiere || info.seriesPremiere,
                IsMovie = showType.IndexOf("Movie", StringComparison.OrdinalIgnoreCase) != -1,
                IsKids = showType.IndexOf("Children", StringComparison.OrdinalIgnoreCase) != -1,
                IsNews = showType.IndexOf("News", StringComparison.OrdinalIgnoreCase) != -1,
                IsSports = showType.IndexOf("Sports", StringComparison.OrdinalIgnoreCase) != -1
            };

            if (!string.IsNullOrWhiteSpace(info.listDateTime))
            {
                program.StartDate = DateTime.ParseExact(info.listDateTime, "yyyy'-'MM'-'dd' 'HH':'mm':'ss", CultureInfo.InvariantCulture);
                program.StartDate = DateTime.SpecifyKind(program.StartDate, DateTimeKind.Utc);
                program.EndDate = program.StartDate.AddMinutes(info.duration);
            }

            if (info.starRating > 0)
            {
                program.CommunityRating = info.starRating*2;
            }

            if (!string.IsNullOrWhiteSpace(info.rating))
            {
                // They don't have dashes so try to normalize
                program.OfficialRating = info.rating.Replace("TV", "TV-").Replace("--", "-");

                var invalid = new[] { "N/A", "Approved", "Not Rated" };
                if (invalid.Contains(program.OfficialRating, StringComparer.OrdinalIgnoreCase))
                {
                    program.OfficialRating = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(info.year))
            {
                program.ProductionYear = int.Parse(info.year, CultureInfo.InvariantCulture);
            }

            if (info.showID > 0)
            {
                program.ShowId = info.showID.ToString(CultureInfo.InvariantCulture);
            }

            if (info.seriesID > 0)
            {
                program.SeriesId = info.seriesID.ToString(CultureInfo.InvariantCulture);
                program.IsSeries = true;
                program.IsRepeat = info.repeat;

                program.EpisodeTitle = GetStringValue(info.episodeTitle);

                if (string.Equals(program.Name, program.EpisodeTitle, StringComparison.OrdinalIgnoreCase))
                {
                    program.EpisodeTitle = null;
                }
            }

            if (info.starRating > 0)
            {
                program.CommunityRating = info.starRating * 2;
            }

            if (string.Equals(info.showName, "Movie", StringComparison.OrdinalIgnoreCase))
            {
                // Sometimes the movie title will be in here
                if (!string.IsNullOrWhiteSpace(info.episodeTitle))
                {
                    program.Name = info.episodeTitle;
                }
            }

            return program;
        }

        private string GetStringValue(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private bool IncludeInResults(ListingInfo info, string itemNumber)
        {
            if (string.Equals(itemNumber, NormalizeNumber(info.number), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var channelNumber = info.channelNumber.ToString(CultureInfo.InvariantCulture);
            if (info.subChannelNumber > 0)
            {
                channelNumber += "." + info.subChannelNumber.ToString(CultureInfo.InvariantCulture);
            }

            return string.Equals(channelNumber, itemNumber, StringComparison.OrdinalIgnoreCase);
        }

        public async Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            var response = await GetResponse<LineupDetailResponse>("https://data.emby.media/service/lineups?id=" + info.ListingsId).ConfigureAwait(false);

            foreach (var channel in channels)
            {
                var station = response.stations.FirstOrDefault(i =>
                {
                    var itemNumber = NormalizeNumber(channel.Number);

                    if (string.Equals(itemNumber, NormalizeNumber(i.number), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    var channelNumber = i.channelNumber.ToString(CultureInfo.InvariantCulture);
                    if (i.subChannelNumber > 0)
                    {
                        channelNumber += "." + i.subChannelNumber.ToString(CultureInfo.InvariantCulture);
                    }

                    return string.Equals(channelNumber, itemNumber, StringComparison.OrdinalIgnoreCase);
                });

                if (station != null)
                {
                    //channel.Name = station.name;

                    if (!string.IsNullOrWhiteSpace(station.logoFilename))
                    {
                        channel.HasImage = true;
                        channel.ImageUrl = "http://cdn.tvpassport.com/image/station/100x100/" + station.logoFilename;
                    }
                }
            }
        }

        private string NormalizeNumber(string number)
        {
            return number.Replace('-', '.');
        }

        public Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            return Task.FromResult(true);
        }

        public async Task<List<NameIdPair>> GetLineups(string country, string location)
        {
            // location = postal code
            var response = await GetResponse<LineupInfo[]>("https://data.emby.media/service/lineups?postalCode=" + location).ConfigureAwait(false);

            return response.Select(i => new NameIdPair
            {
                Name = GetName(i),
                Id = i.lineupID

            }).OrderBy(i => i.Name).ToList();
        }

        private string GetName(LineupInfo info)
        {
            var name = info.lineupName;

            if (string.Equals(info.lineupType, "cab", StringComparison.OrdinalIgnoreCase))
            {
                name += " - Cable";
            }
            else if (string.Equals(info.lineupType, "sat", StringComparison.OrdinalIgnoreCase))
            {
                name += " - SAT";
            }
            else if (string.Equals(info.lineupType, "ota", StringComparison.OrdinalIgnoreCase))
            {
                name += " - OTA";
            }

            return name;
        }

        private async Task<T> GetResponse<T>(string url, Func<string, string> filter = null)
            where T : class
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CacheLength = TimeSpan.FromDays(1),
                CacheMode = CacheMode.Unconditional

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var path = await reader.ReadToEndAsync().ConfigureAwait(false);

                    using (var secondStream = await _httpClient.Get(new HttpRequestOptions
                    {
                        Url = "https://www.mb3admin.com" + path,
                        CacheLength = TimeSpan.FromDays(1),
                        CacheMode = CacheMode.Unconditional

                    }).ConfigureAwait(false))
                    {
                        return ParseResponse<T>(secondStream, filter);
                    }
                }
            }
        }

        private T ParseResponse<T>(Stream response, Func<string,string> filter)
        {
            using (var reader = new StreamReader(response))
            {
                var json = reader.ReadToEnd();

                if (filter != null)
                {
                    json = filter(json);
                }

                return _jsonSerializer.DeserializeFromString<T>(json);
            }
        }

        private class LineupInfo
        {
            public string lineupID { get; set; }
            public string lineupName { get; set; }
            public string lineupType { get; set; }
            public string providerID { get; set; }
            public string providerName { get; set; }
            public string serviceArea { get; set; }
            public string country { get; set; }
        }

        private class Station
        {
            public string number { get; set; }
            public int channelNumber { get; set; }
            public int subChannelNumber { get; set; }
            public int stationID { get; set; }
            public string name { get; set; }
            public string callsign { get; set; }
            public string network { get; set; }
            public string stationType { get; set; }
            public int NTSC_TSID { get; set; }
            public int DTV_TSID { get; set; }
            public string webLink { get; set; }
            public string logoFilename { get; set; }
        }

        private class LineupDetailResponse
        {
            public string lineupID { get; set; }
            public string lineupName { get; set; }
            public string lineupType { get; set; }
            public string providerID { get; set; }
            public string providerName { get; set; }
            public string serviceArea { get; set; }
            public string country { get; set; }
            public List<Station> stations { get; set; }
        }

        private class ListingInfo
        {
            public string number { get; set; }
            public int channelNumber { get; set; }
            public int subChannelNumber { get; set; }
            public int stationID { get; set; }
            public string name { get; set; }
            public string callsign { get; set; }
            public string network { get; set; }
            public string stationType { get; set; }
            public string webLink { get; set; }
            public string logoFilename { get; set; }
            public int listingID { get; set; }
            public string listDateTime { get; set; }
            public int duration { get; set; }
            public int showID { get; set; }
            public int seriesID { get; set; }
            public string showName { get; set; }
            public string episodeTitle { get; set; }
            public string episodeNumber { get; set; }
            public int parts { get; set; }
            public int partNum { get; set; }
            public bool seriesPremiere { get; set; }
            public bool seasonPremiere { get; set; }
            public bool seriesFinale { get; set; }
            public bool seasonFinale { get; set; }
            public bool repeat { get; set; }
            public bool @new { get; set; }
            public string rating { get; set; }
            public bool captioned { get; set; }
            public bool educational { get; set; }
            public bool blackWhite { get; set; }
            public bool subtitled { get; set; }
            public bool live { get; set; }
            public bool hd { get; set; }
            public bool descriptiveVideo { get; set; }
            public bool inProgress { get; set; }
            public string showTypeID { get; set; }
            public int breakoutLevel { get; set; }
            public string showType { get; set; }
            public string year { get; set; }
            public string guest { get; set; }
            public string cast { get; set; }
            public string director { get; set; }
            public int starRating { get; set; }
            public string description { get; set; }
            public string league { get; set; }
            public string team1 { get; set; }
            public string team2 { get; set; }
            public string @event { get; set; }
            public string location { get; set; }
        }
    }
}
