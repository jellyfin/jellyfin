using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.MediaInfo
{
    public class PlaybackInfoRequest
    {
        public DeviceProfile DeviceProfile { get; set; }
        public MediaSourceInfo MediaSource { get; set; }
    }
}
