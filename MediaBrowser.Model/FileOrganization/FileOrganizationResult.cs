using System;

namespace MediaBrowser.Model.FileOrganization
{
    public class FileOrganizationResult
    {
        /// <summary>
        /// Gets or sets the original path.
        /// </summary>
        /// <value>The original path.</value>
        public string OriginalPath { get; set; }

        /// <summary>
        /// Gets or sets the target path.
        /// </summary>
        /// <value>The target path.</value>
        public string TargetPath { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public FileSortingStatus Status { get; set; }
    }

    public enum FileSortingStatus
    {
        Success,
        Failure,
        SkippedExisting,
        SkippedTrial
    }
}
