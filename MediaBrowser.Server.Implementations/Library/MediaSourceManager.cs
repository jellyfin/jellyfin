using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library
{
    public class MediaSourceManager : IMediaSourceManager
    {
        private readonly IItemRepository _itemRepo;

        public MediaSourceManager(IItemRepository itemRepo)
        {
            _itemRepo = itemRepo;
        }

        public IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            var list = _itemRepo.GetMediaStreams(query)
                .ToList();

            foreach (var stream in list)
            {
                stream.SupportsExternalStream = StreamSupportsExternalStream(stream);
            }

            return list;
        }

        private bool StreamSupportsExternalStream(MediaStream stream)
        {
            if (stream.IsExternal)
            {
                return true;
            }

            if (stream.IsTextSubtitleStream)
            {
                return InternalTextStreamSupportsExternalStream(stream);
            }

            return false;
        }

        private bool InternalTextStreamSupportsExternalStream(MediaStream stream)
        {
            return true;
        }

        public IEnumerable<MediaStream> GetMediaStreams(string mediaSourceId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = new Guid(mediaSourceId)
            });

            return GetMediaStreamsForItem(list);
        }

        public IEnumerable<MediaStream> GetMediaStreams(Guid itemId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = itemId
            });

            return GetMediaStreamsForItem(list);
        }

        private IEnumerable<MediaStream> GetMediaStreamsForItem(IEnumerable<MediaStream> streams)
        {
            var list = streams.ToList();

            var subtitleStreams = list
                .Where(i => i.Type == MediaStreamType.Subtitle)
                .ToList();

            if (subtitleStreams.Count > 0)
            {
                var videoStream = list.FirstOrDefault(i => i.Type == MediaStreamType.Video);

                // This is abitrary but at some point it becomes too slow to extract subtitles on the fly
                // We need to learn more about when this is the case vs. when it isn't
                const int maxAllowedBitrateForExternalSubtitleStream = 10000000;

                var videoBitrate = videoStream == null ? maxAllowedBitrateForExternalSubtitleStream : videoStream.BitRate ?? maxAllowedBitrateForExternalSubtitleStream;

                foreach (var subStream in subtitleStreams)
                {
                    var supportsExternalStream = StreamSupportsExternalStream(subStream);

                    if (supportsExternalStream && videoBitrate >= maxAllowedBitrateForExternalSubtitleStream)
                    {
                        supportsExternalStream = false;
                    }

                    subStream.SupportsExternalStream = supportsExternalStream;
                }
            }

            return list;
        }
    }
}
