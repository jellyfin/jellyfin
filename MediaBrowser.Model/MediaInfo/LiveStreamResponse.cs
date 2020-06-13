#pragma warning disable CS1591

using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveStreamResponse
    {
        public LiveStreamResponse(MediaSourceInfo mediaSource)
        {
            MediaSource = mediaSource;
        }

        public MediaSourceInfo MediaSource { get; }
    }
}
