using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Data.Entities;
using MediaBrowser.Model.Activity;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.Dtos
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

        /// <summary>
        /// Explicitly converts from <see cref="ActivityLog"/> to <see cref="ActivityLogDto"/>.
        /// </summary>
        /// <param name="entity">ActivityLog entity.</param>
        public static explicit operator ActivityLogDto(ActivityLog entity)
        {
            return new ActivityLogDto(entity.Name, entity.Type, entity.UserId)
            {
                Overview = entity.Overview,
                ShortOverview = entity.ShortOverview,
                ItemId = entity.ItemId,
                DateCreated = entity.DateCreated,
                LogSeverity = entity.LogSeverity
            };
        }

        /// <summary>
        /// Implicitly converts from <see cref="ActivityLogDto"/> to <see cref="ActivityLog"/>.
        /// </summary>
        /// <param name="dto">ActivityLogDto.</param>
        public static implicit operator ActivityLog(ActivityLogDto dto)
        {
            return new ActivityLog(dto.Name, dto.Type, dto.UserId)
            {
                Overview = dto.Overview,
                ShortOverview = dto.ShortOverview,
                ItemId = dto.ItemId,
                DateCreated = dto.DateCreated,
                LogSeverity = dto.LogSeverity
            };
        }

        /// <summary>
        /// Explicitly convert from <see cref="ActivityLogEntry"/> to <see cref="ActivityLogDto"/>.
        /// </summary>
        /// <param name="entity">ActivityLogEntry entity.</param>
        public static explicit operator ActivityLogDto(ActivityLogEntry entity)
        {
            return new ActivityLogDto(entity.Name, entity.Type, entity.UserId)
            {
                Overview = entity.Overview,
                ShortOverview = entity.ShortOverview,
                ItemId = entity.ItemId,
                DateCreated = entity.Date,
                LogSeverity = entity.Severity
            };
        }

        /// <summary>
        /// Implicitly converts from <see cref="ActivityLogDto"/> to <see cref="ActivityLogEntry"/>.
        /// </summary>
        /// <param name="dto">ActivityLogDto.</param>
        public static implicit operator ActivityLogEntry(ActivityLogDto dto)
        {
            return new ActivityLogEntry(dto.Name, dto.Type, dto.UserId)
            {
                Overview = dto.Overview,
                ShortOverview = dto.ShortOverview,
                ItemId = dto.ItemId,
                Date = dto.DateCreated,
                Severity = dto.LogSeverity
            };
        }
    }
}
