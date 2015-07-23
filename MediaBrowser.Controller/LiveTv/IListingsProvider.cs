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
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, ChannelInfo channel, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);
        Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken);
    }
}
