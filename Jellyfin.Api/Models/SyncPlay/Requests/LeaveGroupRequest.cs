namespace Jellyfin.Api.Models.SyncPlay.Requests
{
    /// <summary>
    /// Class LeaveGroupRequest.
    /// </summary>
    public class LeaveGroupRequest : ISyncPlayRequest
    {
        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.LeaveGroup;
    }
}
