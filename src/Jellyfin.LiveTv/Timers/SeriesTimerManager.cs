#pragma warning disable CS1591

using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Timers
{
    public class SeriesTimerManager : ItemDataProvider<SeriesTimerInfo>
    {
        public SeriesTimerManager(ILogger<SeriesTimerManager> logger, IConfigurationManager config)
            : base(
                logger,
                Path.Combine(config.CommonApplicationPaths.DataPath, "livetv/seriestimers.json"),
                (r1, r2) => r1.Id is not null && r2.Id is not null && r1.Id.Value.Equals(r2.Id.Value))
        {
        }

        /// <inheritdoc />
        public override void Add(SeriesTimerInfo item)
        {
            ArgumentNullException.ThrowIfNull(item.Id);

            base.Add(item);
        }
    }
}
