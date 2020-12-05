namespace Jellyfin.Api.Models.SyncPlay.Requests
{
    /// <summary>
    /// Class ListUsersRequest.
    /// </summary>
    public class ListUsersRequest : ISyncPlayRequest
    {
        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.ListUsers;
    }
}
