#nullable disable

#pragma warning disable CS1591

using System;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public class RecordingStatusChangedEventArgs : EventArgs
    {
        public string RecordingId { get; set; }

        public RecordingStatus NewStatus { get; set; }
    }
}
