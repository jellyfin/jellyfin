using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp
{
    public class ChannelScan
    {
        public async Task<List<SatChannel>> Scan(TunerHostInfo info, CancellationToken cancellationToken)
        {
            return new List<SatChannel>();
        }
    }

    public class SatChannel
    {
        // TODO: Add properties
    }
}
