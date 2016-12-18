using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskTriggerInfo
    /// </summary>
    public class TaskTriggerInfo
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

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
        /// Gets or sets the system event.
        /// </summary>
        /// <value>The system event.</value>
        public SystemEvent? SystemEvent { get; set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the maximum runtime ms.
        /// </summary>
        /// <value>The maximum runtime ms.</value>
        public int? MaxRuntimeMs { get; set; }

        public const string TriggerDaily = "DailyTrigger";
        public const string TriggerWeekly = "WeeklyTrigger";
        public const string TriggerInterval = "IntervalTrigger";
        public const string TriggerSystemEvent = "SystemEventTrigger";
        public const string TriggerStartup = "StartupTrigger";
    }
}
