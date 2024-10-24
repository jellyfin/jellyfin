namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Enum TaskTriggerInfoType.
    /// </summary>
    public enum TaskTriggerInfoType
    {
        /// <summary>
        /// The daily trigger.
        /// </summary>
        DailyTrigger,

        /// <summary>
        /// The weekly trigger.
        /// </summary>
        WeeklyTrigger,

        /// <summary>
        /// The interval trigger.
        /// </summary>
        IntervalTrigger,

        /// <summary>
        /// The startup trigger.
        /// </summary>
        StartupTrigger
    }
}
