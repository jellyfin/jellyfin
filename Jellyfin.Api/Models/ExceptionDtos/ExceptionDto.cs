namespace Jellyfin.Api.Models.ExceptionDtos
{
    /// <summary>
    /// Exception Dto.
    /// Used for graceful handling of API exceptions.
    /// </summary>
    public class ExceptionDto
    {
        /// <summary>
        /// Gets or sets exception message.
        /// </summary>
        public string Message { get; set; }
    }
}
