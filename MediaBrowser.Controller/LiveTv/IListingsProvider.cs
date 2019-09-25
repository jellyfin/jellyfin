using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
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
