using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayRequest.
    /// </summary>
    public interface ISyncPlayRequest
    {
        /// <summary>
        /// Gets the request type.
        /// </summary>
        /// <returns>The request type.</returns>
        RequestType Type { get; }
    }
}
