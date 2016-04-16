using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    public class StreamInfoSorter
    {
        public static List<StreamInfo> SortMediaSources(List<StreamInfo> streams, int? maxBitrate)
        {
            return streams.OrderBy(i =>
            {
                // Nothing beats direct playing a file
                if (i.PlayMethod == PlayMethod.DirectPlay && i.MediaSource.Protocol == MediaProtocol.File)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i =>
            {
                switch (i.PlayMethod)
                {
                    // Let's assume direct streaming a file is just as desirable as direct playing a remote url
                    case PlayMethod.DirectStream:
                    case PlayMethod.DirectPlay:
                        return 0;
                    default:
                        return 1;
                }

            }).ThenBy(i =>
            {
                switch (i.MediaSource.Protocol)
                {
                    case MediaProtocol.File:
                        return 0;
                    default:
                        return 1;
                }

            }).ThenBy(i =>
            {
                if (maxBitrate.HasValue)
                {
                    if (i.MediaSource.Bitrate.HasValue)
                    {
                        if (i.MediaSource.Bitrate.Value <= maxBitrate.Value)
                        {
                            return 0;
                        }

                        return 2;
                    }
                }

                return 1;

            }).ToList();
        }
    }
}
