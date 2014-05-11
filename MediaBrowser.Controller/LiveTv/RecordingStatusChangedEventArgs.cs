using MediaBrowser.Model.LiveTv;
using System;

namespace MediaBrowser.Controller.LiveTv
{
    public class RecordingStatusChangedEventArgs : EventArgs
    {
        public string RecordingId { get; set; }

        public RecordingStatus NewStatus { get; set; }
    }
}
