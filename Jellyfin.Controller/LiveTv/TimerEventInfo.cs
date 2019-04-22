using System;

namespace Jellyfin.Controller.LiveTv
{
    public class TimerEventInfo
    {
        public string Id { get; set; }
        public Guid ProgramId { get; set; }
    }
}
