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
                (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        /// <inheritdoc />
        public override void Add(SeriesTimerInfo item)
        {
            ArgumentException.ThrowIfNullOrEmpty(item.Id);

            base.Add(item);
        }
    }
}
