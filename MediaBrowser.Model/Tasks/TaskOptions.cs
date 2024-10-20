namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class containing options for tasks.
    /// </summary>
    public class TaskOptions
    {
        /// <summary>
        /// Gets or sets the maximum runtime in ticks.
        /// </summary>
        /// <value>The ticks.</value>
        public long? MaxRuntimeTicks { get; set; }
    }
}
