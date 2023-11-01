using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Data.Dtos
{
    /// <summary>
    /// Activity Log dto.
    /// </summary>
    public class ActivityLogDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogDto"/> class.
        /// public constructor with the requried data.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="userId">The user id.</param>
        public ActivityLogDto(string name, string type, Guid userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(type);

            Name = name;
            Type = type;
            UserId = userId;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the userid.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        public string? Overview { get; set; }

        /// <summary>
        /// Gets or sets the short overview.
        /// </summary>
        public string? ShortOverview { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
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
    }
}
