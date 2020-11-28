namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class IgnoreWaitRequestBody.
    /// </summary>
    public class IgnoreWaitRequestBody
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client should be ignored.
        /// </summary>
        /// <value>The client group-wait status.</value>
        public bool IgnoreWait { get; set; }
    }
}
