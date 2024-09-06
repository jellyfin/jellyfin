#nullable disable

#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.Net
{
    public class WebSocketListenerState
    {
        public DateTime DateLastSendUtc { get; set; }

        public long InitialDelayMs { get; set; }

        public long IntervalMs { get; set; }
    }
}
