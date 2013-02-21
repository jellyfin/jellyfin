using ProtoBuf;
using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskTriggerInfo
    /// </summary>
    [ProtoContract]
    public class TaskTriggerInfo
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(1)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the time of day.
        /// </summary>
        /// <value>The time of day.</value>
        [ProtoMember(2)]
        public long? TimeOfDayTicks { get; set; }

        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        /// <value>The interval.</value>
        [ProtoMember(3)]
        public long? IntervalTicks { get; set; }

        /// <summary>
        /// Gets or sets the system event.
        /// </summary>
        /// <value>The system event.</value>
        [ProtoMember(4)]
        public SystemEvent? SystemEvent { get; set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        [ProtoMember(5)]
        public DayOfWeek? DayOfWeek { get; set; }
    }
}
