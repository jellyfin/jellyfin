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
        /// Gets or sets the log message.
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
