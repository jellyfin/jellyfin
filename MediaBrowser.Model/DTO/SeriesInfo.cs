using ProtoBuf;
using System;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class SeriesInfo
    {
        [ProtoMember(1)]
        public string Status { get; set; }

        [ProtoMember(2)]
        public string AirTime { get; set; }

        [ProtoMember(3)]
        public DayOfWeek[] AirDays { get; set; }
    }
}
