#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class AccessSchedule
    {
        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        public DynamicDayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start hour.
        /// </summary>
        /// <value>The start hour.</value>
        public double StartHour { get; set; }

        /// <summary>
        /// Gets or sets the end hour.
        /// </summary>
        /// <value>The end hour.</value>
        public double EndHour { get; set; }
    }
}
