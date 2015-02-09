using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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
    }
}
