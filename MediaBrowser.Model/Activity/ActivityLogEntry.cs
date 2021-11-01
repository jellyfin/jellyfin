using System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.Activity
{
    /// <summary>
    /// An activity log entry.
    /// </summary>
    public class ActivityLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogEntry"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="userId">The user id.</param>
        public ActivityLogEntry(string name, string type, Guid userId)
        {
            Name = name;
            Type = type;
            UserId = userId;
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string? Overview { get; set; }

        /// <summary>
        /// Gets or sets the short overview.
        /// </summary>
        /// <value>The short overview.</value>
        public string? ShortOverview { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string? ItemId { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user primary image tag.
        /// </summary>
        /// <value>The user primary image tag.</value>
        [Obsolete("UserPrimaryImageTag is not used.")]
        public string? UserPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the log severity.
        /// </summary>
        /// <value>The log severity.</value>
        public LogLevel Severity { get; set; }
    }
}
