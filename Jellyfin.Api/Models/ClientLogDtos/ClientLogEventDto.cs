using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Models.ClientLogDtos
{
    /// <summary>
    /// The client log dto.
    /// </summary>
    public class ClientLogEventDto
    {
        /// <summary>
        /// Gets or sets the event timestamp.
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        [Required]
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        [Required]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client version.
        /// </summary>
        [Required]
        public string ClientVersion { get; set; } = string.Empty;

        ///
        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}