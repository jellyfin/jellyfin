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

        public async Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            var response = await GetResponse<LineupInfo[]>("https://data.emby.media/service/lineups?id=" + location).ConfigureAwait(false);

            return response.Select(i => new NameIdPair
            {

                Name = GetName(i),
                Id = i.lineupID

            }).ToList();
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

                    // location = zip code
                    using (var secondStream = await _httpClient.Get(new HttpRequestOptions
                    {
                        Url = "https://data.emby.media" + path

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
    }
}
