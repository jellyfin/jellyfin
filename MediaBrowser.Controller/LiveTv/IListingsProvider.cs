using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface IListingsProvider
    {
        string Name { get; }
        string Type { get; }
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, string channelName, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);
        Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken);
        Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings);
        Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location);
        Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken);
    }
}
