#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class SeriesTimerManager : ItemDataProvider<SeriesTimerInfo>
    {
        public SeriesTimerManager(ILogger logger, string dataPath)
            : base(logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        /// <inheritdoc />
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
