#nullable disable
using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskExecutionInfo.
    /// </summary>
    public class TaskResult
    {
        /// <summary>
        /// Gets or sets the start time UTC.
        /// </summary>
        /// <value>The start time UTC.</value>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the end time UTC.
        /// </summary>
        /// <value>The end time UTC.</value>
        public DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public TaskCompletionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the long error message.
        /// </summary>
        /// <value>The long error message.</value>
        public string LongErrorMessage { get; set; }
    }
}
