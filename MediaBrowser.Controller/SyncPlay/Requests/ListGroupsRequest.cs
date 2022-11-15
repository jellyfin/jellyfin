using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Requests
{
    /// <summary>
    /// Class ListGroupsRequest.
    /// </summary>
    public class ListGroupsRequest : ISyncPlayRequest
    {
        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.ListGroups;
    }
}
