#nullable disable
using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskTriggerInfo.
    /// </summary>
    public class TaskTriggerInfo
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public TaskTriggerInfoType Type { get; set; }

        /// <summary>
        /// Gets or sets the time of day.
        /// </summary>
        /// <value>The time of day.</value>
        public long? TimeOfDayTicks { get; set; }

        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        /// <value>The interval.</value>
        public long? IntervalTicks { get; set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the maximum runtime ticks.
        /// </summary>
        /// <value>The maximum runtime ticks.</value>
        public long? MaxRuntimeTicks { get; set; }
    }
}
