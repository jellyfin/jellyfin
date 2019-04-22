using System;
using Jellyfin.Model.LiveTv;

namespace Jellyfin.Controller.LiveTv
{
    public class RecordingStatusChangedEventArgs : EventArgs
    {
        public string RecordingId { get; set; }

        public RecordingStatus NewStatus { get; set; }
    }
}
