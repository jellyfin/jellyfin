#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.LiveTv
{
    public class TimerEventInfo
    {
        public TimerEventInfo(Guid? id)
        {
            Id = id;
        }

        public Guid? Id { get; }

        public Guid? ProgramId { get; set; }
    }
}
