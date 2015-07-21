using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface IListingsProvider
    {
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ChannelInfo channel, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);
    }
}
