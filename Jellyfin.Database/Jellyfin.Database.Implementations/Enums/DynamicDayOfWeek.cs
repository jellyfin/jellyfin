namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum that represents a day of the week, weekdays, weekends, or all days.
    /// </summary>
    public enum DynamicDayOfWeek
    {
        /// <summary>
        /// Sunday.
        /// </summary>
        Sunday = 0,

        /// <summary>
        /// Monday.
        /// </summary>
        Monday = 1,

        /// <summary>
        /// Tuesday.
        /// </summary>
        Tuesday = 2,

        /// <summary>
        /// Wednesday.
        /// </summary>
        Wednesday = 3,

        /// <summary>
        /// Thursday.
        /// </summary>
        Thursday = 4,

        /// <summary>
        /// Friday.
        /// </summary>
        Friday = 5,

        /// <summary>
        /// Saturday.
        /// </summary>
        Saturday = 6,

        /// <summary>
        /// All days of the week.
        /// </summary>
        Everyday = 7,

        /// <summary>
        /// A week day, or Monday-Friday.
        /// </summary>
        Weekday = 8,

        /// <summary>
        /// Saturday and Sunday.
        /// </summary>
        Weekend = 9
    }
}
