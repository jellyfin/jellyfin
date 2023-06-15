using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity referencing an activity log entry.
    /// </summary>
    public class ActivityLog : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLog"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="userId">The user id.</param>
        public ActivityLog(string name, string type, Guid userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(type);

            Name = name;
            Type = type;
            UserId = userId;
            DateCreated = DateTime.UtcNow;
            LogSeverity = LogLevel.Information;
        }

        /// <summary>
        /// Gets the identity of this instance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 512.
        /// </remarks>
        [MaxLength(512)]
        [StringLength(512)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <remarks>
        /// Max length = 512.
        /// </remarks>
        [MaxLength(512)]
        [StringLength(512)]
        public string? Overview { get; set; }

        /// <summary>
        /// Gets or sets the short overview.
        /// </summary>
        /// <remarks>
        /// Max length = 512.
        /// </remarks>
        [MaxLength(512)]
        [StringLength(512)]
        public string? ShortOverview { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 256.
        /// </remarks>
        [MaxLength(256)]
        [StringLength(256)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <remarks>
        /// Max length = 256.
        /// </remarks>
        [MaxLength(256)]
        [StringLength(256)]
        public string? ItemId { get; set; }

        /// <summary>
        /// Gets or sets the date created. This should be in UTC.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the log severity. Default is <see cref="LogLevel.Trace"/>.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public LogLevel LogSeverity { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
