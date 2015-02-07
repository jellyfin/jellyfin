using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    public interface IMediaSourceManager
    {
        IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query);
    }
}
