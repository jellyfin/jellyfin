#nullable enable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.LiveTv
{
    public class TimerEventInfo
    {
        public TimerEventInfo(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public Guid? ProgramId { get; set; }
    }
}
