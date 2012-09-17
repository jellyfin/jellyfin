
namespace MediaBrowser.Model.Progress
{
    /// <summary>
    /// Represents a generic progress class that can be used with IProgress
    /// </summary>
    public class TaskProgress
    {
        /// <summary>
        /// Gets or sets a description of the actions currently executing
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the current completion percentage
        /// </summary>
        public decimal? PercentComplete { get; set; }
    }
}
