using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
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
            var url = "https://data.emby.media/service/listings?id=" + info.ListingsId;
            url += "&start=" + startDateUtc.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            url += "&end=" + endDateUtc.ToString("s", System.Globalization.CultureInfo.InvariantCulture);

            var response = await GetResponse<ListingInfo[]>(url).ConfigureAwait(false);
            
            return new List<ProgramInfo>();
        }

        public async Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            var response = await GetResponse<LineupDetailResponse>("https://data.emby.media/service/lineups?id=" + info.ListingsId).ConfigureAwait(false);

            foreach (var channel in channels)
            {

            }
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

        private async Task<T> GetResponse<T>(string url)
            where T : class
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var path = await reader.ReadToEndAsync().ConfigureAwait(false);

                    using (var secondStream = await _httpClient.Get(new HttpRequestOptions
                    {
                        Url = "https://www.mb3admin.com" + path

                    }).ConfigureAwait(false))
                    {
                        return _jsonSerializer.DeserializeFromStream<T>(secondStream);
                    }
                }
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
