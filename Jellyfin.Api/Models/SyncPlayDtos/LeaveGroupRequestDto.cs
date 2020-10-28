namespace Jellyfin.Api.Models.SyncPlayDtos
{
    /// <summary>
    /// Class LeaveGroupRequestDto.
    /// </summary>
    public class LeaveGroupRequestDto
    {
        /// <summary>
        /// Gets or sets the identifier of the remote session that will leave the group instead.
        /// </summary>
        /// <value>The identifier of the remote session.</value>
        public string? RemoteSessionId { get; set; }
    }
}
