using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Requests
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
