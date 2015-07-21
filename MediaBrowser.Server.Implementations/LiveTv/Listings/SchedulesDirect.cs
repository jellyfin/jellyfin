using MediaBrowser.Controller.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings
{
    public class SchedulesDirect : IListingsProvider
    {
        public Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ChannelInfo channel, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
