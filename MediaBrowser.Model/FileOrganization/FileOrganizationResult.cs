using System;

namespace MediaBrowser.Model.FileOrganization
{
    public class FileOrganizationResult
    {
        /// <summary>
        /// Gets or sets the result identifier.
        /// </summary>
        /// <value>The result identifier.</value>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the original path.
        /// </summary>
        /// <value>The original path.</value>
        public string OriginalPath { get; set; }

        /// <summary>
        /// Gets or sets the name of the original file.
        /// </summary>
        /// <value>The name of the original file.</value>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the extracted.
        /// </summary>
        /// <value>The name of the extracted.</value>
        public string ExtractedName { get; set; }

        /// <summary>
        /// Gets or sets the extracted year.
        /// </summary>
        /// <value>The extracted year.</value>
        public int? ExtractedYear { get; set; }

        /// <summary>
        /// Gets or sets the extracted season number.
        /// </summary>
        /// <value>The extracted season number.</value>
        public int? ExtractedSeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the extracted episode number.
        /// </summary>
        /// <value>The extracted episode number.</value>
        public int? ExtractedEpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the extracted ending episode number.
        /// </summary>
        /// <value>The extracted ending episode number.</value>
        public int? ExtractedEndingEpisodeNumber { get; set; }
        
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
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public FileSortingStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public FileOrganizerType Type { get; set; }
    }

    public enum FileSortingStatus
    {
        Success,
        Failure,
        SkippedExisting
    }

    public enum FileOrganizerType
    {
        Movie,
        Episode,
        Song
    }
}
