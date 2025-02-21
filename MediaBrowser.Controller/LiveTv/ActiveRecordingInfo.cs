#nullable disable

#pragma warning disable CS1591

using System.Threading;

namespace MediaBrowser.Controller.LiveTv
{
    public class ActiveRecordingInfo
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public TimerInfo Timer { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
