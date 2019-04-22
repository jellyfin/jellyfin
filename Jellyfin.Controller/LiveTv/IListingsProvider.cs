using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Model.Dto;
using Jellyfin.Model.LiveTv;

namespace Jellyfin.Controller.LiveTv
{
    public interface IListingsProvider
    {
        string Name { get; }
        string Type { get; }
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);
        Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings);
        Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location);
        Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken);
    }
}
