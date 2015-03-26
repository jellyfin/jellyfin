using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    public class StreamInfoSorter
    {
        public static List<StreamInfo> SortMediaSources(List<StreamInfo> streams)
        {
            return streams.OrderBy(i =>
            {
                switch (i.PlayMethod)
                {
                    case PlayMethod.DirectPlay:
                        return 0;
                    case PlayMethod.DirectStream:
                        return 1;
                    case PlayMethod.Transcode:
                        return 2;
                    default:
                        throw new ArgumentException("Unrecognized PlayMethod");
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

            }).ToList();
        }
    }
}
