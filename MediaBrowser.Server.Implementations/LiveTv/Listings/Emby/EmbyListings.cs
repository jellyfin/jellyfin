using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings.Emby
{
    public class EmbyGuide : IListingsProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public EmbyGuide(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public string Name
        {
            get { return "Emby Guide"; }
        }

        public string Type
        {
            get { return "emby"; }
        }

        public Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, string channelName, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            return GetListingsProvider(info.Country).GetProgramsAsync(info, channelNumber, startDateUtc, endDateUtc, cancellationToken);
        }

        public Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            return GetListingsProvider(info.Country).AddMetadata(info, channels, cancellationToken);
        }

        public Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            return GetListingsProvider(info.Country).Validate(info, validateLogin, validateListings);
        }

        public Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            return GetListingsProvider(country).GetLineups(country, location);
        }

        private IEmbyListingProvider GetListingsProvider(string country)
        {
            return new EmbyListingsNorthAmerica(_httpClient, _jsonSerializer);
        }
    }
}
