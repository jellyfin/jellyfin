using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using CommonIO;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class SeriesTimerManager : ItemDataProvider<SeriesTimerInfo>
    {
        public SeriesTimerManager(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(fileSystem, jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        public override void Add(SeriesTimerInfo item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("SeriesTimerInfo.Id cannot be null or empty.");
            }

            base.Add(item);
        }
    }
}
