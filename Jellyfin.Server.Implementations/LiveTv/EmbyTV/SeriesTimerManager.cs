using System;
using Jellyfin.Controller.LiveTv;
using Jellyfin.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.LiveTv.EmbyTV
{
    public class SeriesTimerManager : ItemDataProvider<SeriesTimerInfo>
    {
        public SeriesTimerManager(IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        public override void Add(SeriesTimerInfo item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                throw new ArgumentException("SeriesTimerInfo.Id cannot be null or empty.");
            }

            base.Add(item);
        }
    }
}
