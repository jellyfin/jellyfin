using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings.Emby
{
    public interface IEmbyListingProvider
    {
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);
        Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken);
        Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings);
        Task<List<NameIdPair>> GetLineups(string country, string location);
    }
}
